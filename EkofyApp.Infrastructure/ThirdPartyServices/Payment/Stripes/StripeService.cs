using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Coupons;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Stripe;
using Stripe.Checkout;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Payment.Stripes;
public sealed class StripeService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService, IHttpContextAccessor httpContextAccessor, ILogger<StripeService> logger) : IStripeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<StripeService> _logger = logger;

    /// <summary>
    /// Nạp tiền test vào Available Balance (sandbox).
    /// </summary>
    public async Task<PaymentIntent> CreateTopupAsync(long amount, string currency = "usd")
    {
        return await new PaymentIntentService().CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = amount,
            Currency = currency,
            PaymentMethod = "pm_card_bypassPendingInternational", // hoặc tok_bypassPending, tok_bypassPendingInternational, pm_card_visa
            Confirm = true,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never", // tránh yêu cầu return_url
            }
        });
    }

    public Balance GetBalance()
    {
        var balanceService = new BalanceService();
        return balanceService.Get();
    }

    // Tạo Connected Account cho Artist
    public async Task<Account> CreateExpressConnectedAccountTest()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string email = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.Email)
            .FirstOrDefaultAsync();

        AccountService accountService = new();
        Account account = accountService.Create(new AccountCreateOptions
        {
            Type = "express",  // Express phổ biến nhất
            Country = "SG",   // Sandbox test US/EU (VN không hỗ trợ)
            Email = email,
            DefaultCurrency = CurrencyType.sgd.ToString(),
            Settings = new AccountSettingsOptions
            {
                Payouts = new AccountSettingsPayoutsOptions
                {
                    Schedule = new AccountSettingsPayoutsScheduleOptions
                    {
                        Interval = "manual" // Chuyển tiền thủ công
                        //Interval = "monthly", // Tự động chuyển tiền hàng tháng,
                        //DelayDays = 3, // sau 3 ngày
                        //MonthlyPayoutDays = [28, 31], // vào ngày 28 và 31 hàng tháng
                    }
                },
            },
        });

        return account;
    }

    public async Task CreateExpressConnectedAccount()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        User user = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id)
                .Include(x => x.Email)
                .Include(x => x.FullName)
                .Include(x => x.BirthDate)
                .Include(x => x.PhoneNumber)
                .Include(x => x))
            .FirstOrDefaultAsync();

        // Tách tên
        string[] names = user.FullName.Split(' ');
        string lastName = names[^1];
        string firstName = string.Join(" ", names.Take(names.Length - 1));

        // Chuẩn hóa số điện thoại Singapore
        string singaporePhone = user.PhoneNumber!.StartsWith("0")
            ? string.Concat("+65", user.PhoneNumber.AsSpan(2))
            : "+65" + user.PhoneNumber;

        Artist artist = await _unitOfWork.GetCollection<Artist>()
            .Find(x => x.UserId == userId)
            .Project<Artist>(Builders<Artist>.Projection
                .Include(x => x.IdentityCard))
            .FirstOrDefaultAsync();

        AccountService accountService = new();
        Account account = accountService.Create(new AccountCreateOptions
        {
            Type = "express",  // Express phổ biến nhất
            Country = "SG",   // Sandbox test US/EU (VN không hỗ trợ)
            Email = user.Email,
            DefaultCurrency = CurrencyType.sgd.ToString(),
            BusinessType = "individual",
            //TosAcceptance = new AccountTosAcceptanceOptions
            //{
            //    Date = HelperMethod.GetUtcPlus7TimeOffset().DateTime,
            //    Ip = "127.0.0.1"
            //},
            Capabilities = new AccountCapabilitiesOptions
            {
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
            },
            BusinessProfile = new AccountBusinessProfileOptions
            {
                Mcc = "5815", // Mã MCC cho "Computer Software Stores"
                ProductDescription = "Platform for artists to share and monetize their work.",
            },

            Individual = new AccountIndividualOptions()
            {
                FirstName = firstName,
                LastName = lastName,
                Email = user.Email,
                Phone = singaporePhone,
                IdNumber = "000000000", // Dùng số này sẽ tự pass KYC
                Dob = new DobOptions
                {
                    Day = user.BirthDate.Day,
                    Month = user.BirthDate.Month,
                    Year = user.BirthDate.Year
                },
                Address = new AddressOptions
                {
                    Line1 = "address_full_match",
                    City = "Singapore",
                    Country = "SG",
                    PostalCode = "238838"
                },
                Relationship = new AccountIndividualRelationshipOptions
                {
                    Owner = true,
                    Title = "Artist",
                    PercentOwnership = 100,
                    Director = true,
                    Executive = true,
                },
                FullNameAliases = null,
                Verification = new AccountIndividualVerificationOptions
                {
                    Document = new AccountIndividualVerificationDocumentOptions
                    {
                        Front = "file_identity_document_success" // Test token để pass
                    },
                    // If you need to set AdditionalDocument, do so here
                    // AdditionalDocument = new AccountIndividualVerificationAdditionalDocumentOptions { ... }
                }
            },
            //ExtraParams = new Dictionary<string, object>
            //{
            //    { "nationality", "SG" }, // ISO 3166-1 alpha-2 country code
            //    { "full_name_aliases", Array.Empty<string>() } // nếu bạn không muốn dùng thuộc tính có sẵn
            //},
            Settings = new AccountSettingsOptions
            {
                Payouts = new AccountSettingsPayoutsOptions
                {
                    Schedule = new AccountSettingsPayoutsScheduleOptions
                    {
                        Interval = "manual" // Rút tiền thủ công
                        //Interval = "monthly", // Tự động rút tiền hàng tháng,
                        //DelayDays = 3, // sau 3 ngày
                        //MonthlyPayoutDays = [28, 31], // vào ngày 28 và 31 hàng tháng
                    }
                },
            },
        });

        UpdateResult updateResult = await _unitOfWork.GetCollection<User>()
            .UpdateOneAsync(
                Builders<User>.Filter.Eq(x => x.Id, userId),
                Builders<User>.Update.Set(x => x.StripeAccountId, account.Id)
            );

        if (updateResult.ModifiedCount == 0)
        {
            throw new NotFoundCustomException("Cannot create express connected account.");
        }

        // Thêm tài khoản ngân hàng Việt Nam
        AccountExternalAccountCreateOptions bankAccountOptions = new()
        {
            ExternalAccount = new AccountExternalAccountBankAccountOptions
            {
                Country = "SG",
                Currency = CurrencyType.sgd.ToString(),
                AccountHolderName = user.FullName,
                AccountHolderType = "individual",
                RoutingNumber = "1100-000", // 8 chữ số
                AccountNumber = "000123456" // 1-17 chữ số
            }
        };

        AccountExternalAccountService externalAccountService = new();
        await externalAccountService.CreateAsync(account.Id, bankAccountOptions);

        return;
    }

    // Tạo link onboarding để Artist nhập thông tin
    public AccountLink CreateAccountOnboardingLinkTest(string refreshUrl, string returnUrl)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string userStripeAccountId = _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.StripeAccountId)
            .FirstOrDefault() ?? throw new NotFoundCustomException("Not found your Stripe Account ID. Please contact us for more information.");

        AccountLinkService accountLinkService = new();
        return accountLinkService.Create(new AccountLinkCreateOptions
        {
            Account = userStripeAccountId,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding"
        });
    }

    public AccountLinkResponse CreateAccountOnboardingLink(string refreshUrl, string returnUrl)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string userStripeAccountId = _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.StripeAccountId)
            .FirstOrDefault() ?? throw new NotFoundCustomException("Not found your Stripe Account ID. Please contact us for more information.");

        AccountLinkService accountLinkService = new();
        AccountLink accountLink = accountLinkService.Create(new AccountLinkCreateOptions
        {
            Account = userStripeAccountId,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding"
        });

        return new()
        {
            AccountId = userStripeAccountId,
            Url = accountLink.Url,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding",
            Created = accountLink.Created,
        };
    }

    // Tạo Customer
    // TODO: Lưu customerId vào DB User nhưng chỉ khi user thanh toán thành công lần đầu
    // Resolved: Đã lưu ngay khi thanh toán thành công lần đầu
    public async Task<Customer> CreateCustomerAsync()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        User user = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Email)
                .Include(x => x.FullName))
            .FirstOrDefaultAsync();

        CustomerService customerService = new();
        return customerService.Create(new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.FullName,
            //PaymentMethod = "pm_card_visa", // test tạm
        });
    }

    public async Task<bool> IsCustomerIdExisted()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string? customerId = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.StripeCustomerId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(customerId))
        {
            return false;
        }

        return true;
    }

    // Tạo Checkout Session (link) cho thanh toán 1 lần
    public async Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreatePaymentCheckoutSessionRequest createPaymentCheckoutSessionRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        User user = _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.StripeCustomerId)
                .Include(x => x.Email))
            .FirstOrDefault() ?? throw new NotFoundCustomException("Not found your Stripe Customer ID. Please contact us for more information.");

        ArtistPackage artistPackage = await _unitOfWork.GetCollection<ArtistPackage>()
            .Find(x => x.Id == createPaymentCheckoutSessionRequest.PackageId && x.Status == ArtistPackageStatus.Enabled)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any artist package.");

        string platformFeePercentage = await _redisCacheService.HashGetAsync("escrow_commission_policy:active", "platform_fee_percentage") ?? await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .Project(x => x.PlatformFeePercentage.ToString())
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found active escrow commission policy.");

        // Lấy coupon giảm giá nếu có
        // TODO: Cần sửa lại nếu không phải yearly thì không lấy coupon
        //List<string> couponIds = [];
        //if (createCheckoutSessionRequest.Period == PeriodTime.year)
        //{
        //    //couponIds = await _unitOfWork.GetCollection<EntityCoupon>()
        //    //    .Find(x => createCheckoutSessionRequest.CouponCodes.Contains(x.Code) && x.Status == CouponStatus.Active)
        //    //    .Project(x => x.StripeCouponId)
        //    //    .ToListAsync();

        //    couponIds = ["npw1701t"];
        //}

        CheckoutOption.SessionCreateOptions options = new()
        {
            PaymentMethodTypes = ["card", "link"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = artistPackage.Currency.ToString(),
                        UnitAmountDecimal = artistPackage.Amount,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = artistPackage.PackageName,
                            Description = artistPackage.Description,
                        }
                    },
                    Quantity = 1,
                },
            ],
            Customer = user.StripeCustomerId, // có thể truyền customerId nếu đã có
            Mode = "payment",
            SuccessUrl = createPaymentCheckoutSessionRequest.SuccessUrl,
            CancelUrl = createPaymentCheckoutSessionRequest.CancelUrl,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ReceiptEmail = createPaymentCheckoutSessionRequest.IsReceiptEmail ? user.Email : null, // Gửi biên lai về email của customer
                SetupFutureUsage = createPaymentCheckoutSessionRequest.IsSavePaymentMethod ? "off_session" : null, // Lưu thẻ để thanh toán các lần sau
            },
            Metadata = new Dictionary<string, string>
            {
                { "is_subscription", "false" },
                { "package_id", artistPackage.Id },
                // Client Id = User Id từ payment transaction
                { "provider_id", artistPackage.ArtistId },
                { "conversation_id", createPaymentCheckoutSessionRequest.ConversationId },
                { "package_order_description", createPaymentCheckoutSessionRequest.PackageOrderDescription ?? string.Empty },
                { "deadline", HelperMethod.NormalizeToStringUtcPlus7(createPaymentCheckoutSessionRequest.Deadline) },
                { "platform_fee_percentage", platformFeePercentage },
                // Deliveries thì không cần
                { "package_name", artistPackage.PackageName },
                { "package_amount", artistPackage.Amount.ToString() },
                { "package_currency", artistPackage.Currency.ToString() },
                { "package_description", artistPackage.Description ?? string.Empty },
                { "package_status", artistPackage.Status.ToString() },
            },
            //InvoiceCreation = new SessionInvoiceCreationOptions
            //{
            //    Enabled = true // Tạo hóa đơn cho thanh toán
            //},
            //Discounts = couponIds.Select(x => new SessionDiscountOptions
            //{
            //    Coupon = x
            //}).ToList(),
        };

        CheckoutOption.SessionService service = new();
        CheckoutOption.Session checkoutSession = service.Create(options);
        if (string.IsNullOrEmpty(checkoutSession.Url))
        {
            throw new NotFoundCustomException("Error while generating URL for checkout session");
        }

        await _unitOfWork.GetCollection<PaymentTransaction>().InsertOneAsync(new PaymentTransaction
        {
            UserId = userId,
            StripeCheckoutSessionId = checkoutSession.Id,
            StripePaymentId = checkoutSession.PaymentIntentId, // Lúc này chưa có paymentId nên null
            StripePaymentMethod = checkoutSession.PaymentMethodTypes,
            Amount = Convert.ToDecimal(checkoutSession.AmountTotal),
            Currency = checkoutSession.Currency,

            PaymentStatus = PaymentTransactionStatus.Pending,
            Status = TransactionStatus.Open
        });

        return new()
        {
            Id = checkoutSession.Id,
            Url = checkoutSession.Url,
            SuccessUrl = checkoutSession.SuccessUrl,
            CancelUrl = checkoutSession.CancelUrl,
            Status = checkoutSession.Status,
            Mode = checkoutSession.Mode,
            Created = checkoutSession.Created,
            Expired = checkoutSession.ExpiresAt,
        };
    }

    //public async Task RefundAsync(string paymentIntentId, decimal amount, string reason = "requested_by_customer")
    //{
    //    // Lấy PaymentIntent để lấy currency
    //    PaymentIntentService paymentIntentService = new();
    //    PaymentIntent paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
    //    RefundService refundService = new();
    //    await refundService.CreateAsync(new RefundCreateOptions
    //    {
    //        PaymentIntent = paymentIntentId,
    //        Amount = HelperCurrencyConverter.aaaaaa(amount, paymentIntent.Currency),
    //        Reason = reason,
    //    });
    //}

    // Tạo Checkout Session (link) cho đăng ký gói (subscription)
    public async Task<CheckoutSessionResponse> CreateSubscriptionCheckoutSession(CreateSubscriptionCheckoutSessionRequest createCheckoutSessionRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        User user = _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Role)
                .Include(x => x.StripeCustomerId)
                .Include(x => x.Email))
            .FirstOrDefault() ?? throw new NotFoundCustomException("Session is limit. Please login again.");

        // Lấy subscriptionId từ subscriptionTier và subscriptionVersion
        // Tạm thời chưa Lookup vì chưa hoàn thành Entity SubscriptionPlan
        // TODO: Cần lookup SubscriptionPlan để lấy StripePriceId
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Code == createCheckoutSessionRequest.SubscriptionCode &&
                x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Code))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any subscription.");

        SubscriptionPlan subscriptionPlan = await _unitOfWork.GetCollection<SubscriptionPlan>()
            .Find(x => x.SubscriptionId == subscription.Id && x.StripeProductActive == true)
            .Project<SubscriptionPlan>(Builders<SubscriptionPlan>.Projection
                .Include(x => x.Id)
                .Include(x => x.SubscriptionPlanPrices)
                .Include(x => x.StripeProductId)
                .Include(x => x.StripeProductActive)
                .Include(x => x.StripeProductName)
                .Include(x => x.StripeProductImages)
                .Include(x => x.StripeProductType)
                .Include(x => x.StripeProductMetadata)
                .ElemMatch(x => x.SubscriptionPlanPrices, p => p.Interval == createCheckoutSessionRequest.Period))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription plan's price.");

        // Lấy coupon giảm giá nếu có
        List<string>? couponIds = [];
        if (createCheckoutSessionRequest.Period == PeriodTime.year)
        {
            couponIds = await _unitOfWork.GetCollection<EntityCoupon>()
                .Find(x => x.Purpose == CouponPurposeType.AnnualPlanDiscount && x.Status == CouponStatus.Active)
                .Project(x => x.StripeCouponId)
                .ToListAsync();
        }
        else
        {
            couponIds = null;
        }

        CheckoutOption.SessionCreateOptions options = new()
        {
            PaymentMethodTypes = ["card", "link"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = subscriptionPlan.SubscriptionPlanPrices.First().StripePriceId, // ID gói đã tạo trong Stripes
                    Quantity = 1,
                },
            ],
            Customer = user.StripeCustomerId, // có thể truyền customerId nếu đã có
            Mode = "subscription",
            //OriginContext = "web",
            SuccessUrl = createCheckoutSessionRequest.SuccessUrl,
            CancelUrl = createCheckoutSessionRequest.CancelUrl,
            InvoiceCreation = new SessionInvoiceCreationOptions
            {
                Enabled = true, // Tạo hóa đơn
                //InvoiceData = new SessionInvoiceCreationInvoiceDataOptions
                //{
                //    Description = $"Invoice for {subscriptionPlan.Name} plan",
                //}
            },
            Discounts = couponIds != null ? couponIds?.Select(x => new SessionDiscountOptions
            {
                Coupon = x
            }).ToList() : null,
            //Metadata = new Dictionary<string, string>
            //{
            //    { "interval", createCheckoutSessionRequest.Period.ToString() },
            //    { "intervalCount",  subscriptionPlan.SubscriptionPlanPrices.First().IntervalCount.ToString()}
            //},
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId },
                    { "user_role", user.Role.ToString() },
                    { "subscription_id", subscription.Id },
                    { "interval", createCheckoutSessionRequest.Period.ToString() },
                    { "intervalCount",  subscriptionPlan.SubscriptionPlanPrices.First().IntervalCount.ToString()}
                }
            },
            Metadata = new Dictionary<string, string>
            {
                { "is_subscription", "true" },
                { "subscription_code", subscription.Code },
                { "subscription_period", createCheckoutSessionRequest.Period.ToString() },
            },
        };

        CheckoutOption.SessionService service = new();
        CheckoutOption.Session checkoutSession = service.Create(options);
        if (string.IsNullOrEmpty(checkoutSession.Url))
        {
            throw new NotFoundCustomException("Error while generating URL for checkout session");
        }

        await _unitOfWork.GetCollection<PaymentTransaction>().InsertOneAsync(new PaymentTransaction
        {
            UserId = userId,
            StripeCheckoutSessionId = checkoutSession.Id,
            StripePaymentId = checkoutSession.PaymentIntentId, // Lúc này chưa có paymentId nên null
            StripePaymentMethod = checkoutSession.PaymentMethodTypes,
            Amount = Convert.ToDecimal(checkoutSession.AmountTotal),
            Currency = checkoutSession.Currency,

            PaymentStatus = PaymentTransactionStatus.Pending,
            Status = TransactionStatus.Open
        });

        return new()
        {
            Id = checkoutSession.Id,
            Url = checkoutSession.Url,
            SuccessUrl = checkoutSession.SuccessUrl,
            CancelUrl = checkoutSession.CancelUrl,
            Status = checkoutSession.Status,
            Mode = checkoutSession.Mode,
            Created = checkoutSession.Created,
            Expired = checkoutSession.ExpiresAt,
        };
    }

    public async Task CancelSubscriptionAtPeriodEndAsync()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string stripeSubscriptionId = await _unitOfWork.GetCollection<UserSubscription>()
            .Find(x => x.UserId == userId && x.IsActive == true)
            .Project(x => x.StripeSubscriptionId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found your current subscription.");

        SubscriptionService subscriptionService = new();
        await subscriptionService.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        });
    }

    public async Task ResumeSubscriptionAsync()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        UserSubscription userSubscription = await _unitOfWork.GetCollection<UserSubscription>()
            .Find(x => x.UserId == userId && x.IsActive == true)
            .Project<UserSubscription>(Builders<UserSubscription>.Projection
                .Include(x => x.StripeSubscriptionId)
                .Include(x => x.PeriodEnd))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found your current subscription.");

        if (userSubscription.PeriodEnd < HelperMethod.GetUtcPlus7TimeOffset().AddDays(3))
        {
            throw new ConflictCustomException("You can only resume your subscription at least 3 days before it ends.");
        }

        SubscriptionService subscriptionService = new();
        await subscriptionService.UpdateAsync(userSubscription.StripeSubscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = false
        });
    }

    // Giải ngân tiền từ Platform sang Artist
    public async Task EscrowReleaseAsync(string packageOrderId)
    {
        // Tìm payment transaction trong package order chưa giải ngân
        PackageOrder packageOrder = await _unitOfWork.GetCollection<PackageOrder>()
            .Find(x => x.Id == packageOrderId && x.CompletedAt != null)
            .Project<PackageOrder>(Builders<PackageOrder>.Projection
                .Include(x => x.PaymentTransactionId)
                .Include(x => x.ProviderId))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found package order.");

        decimal amountPackageOrder = await _unitOfWork.GetCollection<PaymentTransaction>()
            .Find(x => x.Id == packageOrder.PaymentTransactionId)
            .Project(x => x.Amount)
            .FirstOrDefaultAsync();

        if (amountPackageOrder <= 0)
        {
            throw new ConflictCustomException("Amount in package order is invalid.");
        }

        // Thực hiện chuyển tiền cho Artist
        string artistStripeAccountId = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == packageOrder.ProviderId)
            .Project(x => x.StripeAccountId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found artist's Stripe Account ID.");

        long stripeAmountPackageOrder = HelperCurrencyConverter.ConvertVndDecimalToStripeAmountSgdLong(amountPackageOrder);
        TransferResponse transferResponse = TransferToArtist(artistStripeAccountId, stripeAmountPackageOrder, $"Transfer escrow for package order {packageOrder.Id}"); // chuyển tiền

        // Payout cho Artist
        // Đợi một chút để transfer được xử lý
        await Task.Delay(3000);

        // Kiểm tra balance của connected account trước khi payout
        Balance accountBalance = await GetConnectedAccountBalanceAsync(artistStripeAccountId);
        long availableBalance = accountBalance.Available.FirstOrDefault()?.Amount ?? 0;

        if (availableBalance < stripeAmountPackageOrder)
        {
            throw new ConflictCustomException($"Available balance {availableBalance} is insufficient for payout amount {stripeAmountPackageOrder} to connected account {artistStripeAccountId} for package order {packageOrder.Id}.");
        }

        Dictionary<string, string> metadata = new()
        {
            { "package_order_id", packageOrder.Id },
        };
        Payout payoutResponse = await CreateStandardPayoutAsync(artistStripeAccountId, stripeAmountPackageOrder, $"Payout escrow for package order {packageOrder.Id}", metadata);

        // Tạo payout transaction
        await _unitOfWork.GetCollection<PayoutTransaction>().InsertOneAsync(new PayoutTransaction
        {
            UserId = packageOrder.ProviderId,
            StripeTransferId = transferResponse.Id,
            StripePayoutId = payoutResponse.Id,
            Amount = amountPackageOrder,
            Currency = CurrencyType.vnd.ToString(),
            DestinationAccountId = artistStripeAccountId,
            Description = payoutResponse.Description,
            Status = Enum.Parse<PayoutTransactionStatus>(payoutResponse.Status), // pending, in_transit
            Method = payoutResponse.Method, // standard hoặc instant
        });
    }

    // Chuyển tiền cho Artist
    // TODO: Cần cân nhắc hàm này được sử dụng thế nào, khi nào, ở đâu
    // Nên dùng void
    public TransferResponse TransferToArtist(string artistAccountId, long amount, string description)
    {
        TransferService transferService = new();
        Transfer transfer = transferService.Create(new TransferCreateOptions
        {
            Amount = amount,
            Currency = CurrencyType.sgd.ToString(),
            Destination = artistAccountId,

            Description = description,
        });

        return new TransferResponse()
        {
            Id = transfer.Id,
            Amount = transfer.Amount!,
            Currency = transfer.Currency,
            DestinationAccountId = transfer.DestinationId,
            Description = transfer.Description,
        };
    }

    // Chuyển tiền cho nhiều Artist
    // TODO: Cần cân nhắc hàm này được sử dụng thế nào, khi nào, ở đâu
    // Nên dùng void
    public void TransferGroupArtist(string[] artistAccountIds, long amount, string groupId = "default")
    {
        TransferService transferService = new();
        foreach (string artistAccountId in artistAccountIds)
        {
            transferService.Create(new TransferCreateOptions
            {
                Amount = amount,
                Currency = CurrencyType.sgd.ToString(),
                Destination = artistAccountId,
                TransferGroup = groupId,
                Description = "Royalty payout for streaming"
            });
        }

        return;
    }

    /// <summary>
    /// Dùng API này để xóa tài khoản connected account vì trên dashboard không có.
    /// </summary>
    /// <param name="accountId"></param>
    /// <returns></returns>
    public async Task DeleteConnectedAccount(string accountId)
    {
        await new AccountService().DeleteAsync(accountId);
    }

    #region Không dùng tới vì chả biết làm gì với nó

    // Dùng cho thanh toán 1 lần (mua gói của nghệ sĩ)
    public PaymentIntent CreateOncePaymentIntent(long amount, string currency = "vnd")
    {
        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
        {
            Amount = amount,
            Currency = currency,
            PaymentMethodTypes = ["card", "link"] // thêm nhiều phương thức
        });

        return paymentIntent;
    }

    public StripeSubscription CreateSubscription(string customerId, string priceId)
    {
        var subscriptionService = new SubscriptionService();
        var subscription = subscriptionService.Create(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items =
            [
                new SubscriptionItemOptions { Price = priceId }
            ],

            CollectionMethod = "charge_automatically",
        });

        return subscription;
    }

    // 5. Tạo PaymentIntent cho nhiều lần thanh toán
    public PaymentIntent CreatePaymentIntent(long amount, string customerId, string paymentMethodId, string currency = "vnd")
    {
        var paymentIntentService = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = amount,
            Currency = currency,
            Customer = customerId,
            PaymentMethod = paymentMethodId,
            PaymentMethodTypes = [PaymentMethodType.Card.ToString()],
            Confirm = true, // Tự confirm luôn
            OffSession = false // true nếu muốn thanh toán không cần user nhập lại
        };

        return paymentIntentService.Create(options);
    }

    public void AttachPaymentMethodToCustomer(string customerId, string paymentMethodId)
    {
        var service = new PaymentMethodService();
        service.Attach(paymentMethodId, new PaymentMethodAttachOptions
        {
            Customer = customerId
        });

        // Đặt làm default
        var customerService = new CustomerService();
        customerService.Update(customerId, new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = paymentMethodId
            }
        });
    }

    public PaymentMethod GetPaymentMethod(string paymentMethodId)
    {
        var service = new PaymentMethodService();
        return service.Get(paymentMethodId);
    }
    #endregion

    #region Payout Methods
    /// <summary>
    /// Tạo payout thường (1-5 ngày làm việc) cho connected account
    /// </summary>
    public async Task<Payout> CreateStandardPayoutAsync(string connectedAccountId, long amount, string? description = null, Dictionary<string, string>? metadata = null, string currency = "sgd")
    {
        string alternativeDescription = $"Royalty payout - {HelperMethod.GetUtcPlus7TimeOffset():MM-yyyy}";

        PayoutService payoutService = new();

        RequestOptions requestOptions = new()
        {
            StripeAccount = connectedAccountId
        };

        return await payoutService.CreateAsync(new PayoutCreateOptions
        {
            Amount = amount,
            Currency = currency,
            Method = "standard", // Standard payout (1-5 business days)
            Description = description ?? alternativeDescription,
            Metadata = metadata ?? new Dictionary<string, string>
            {
                { "empty", "empty" }
            }
        }, requestOptions);
    }

    /// <summary>
    /// Tạo instant payout (trong vòng 30 phút, có phí cao hơn)
    /// </summary>
    public async Task<Payout> CreateInstantPayoutAsync(string connectedAccountId, long amount, string? description = null, string currency = "sgd")
    {
        string alternativeDescription = $"Instant royalty payout - {HelperMethod.GetUtcPlus7TimeOffset():MM-yyyy}";

        PayoutService payoutService = new();

        RequestOptions requestOptions = new()
        {
            StripeAccount = connectedAccountId
        };

        return await payoutService.CreateAsync(new PayoutCreateOptions
        {
            Amount = amount,
            Currency = currency,
            Method = "instant", // Instant payout (within 30 minutes, higher fee)
            Description = description ?? alternativeDescription
        }, requestOptions);
    }

    public async Task<Balance> GetConnectedAccountBalanceAsync(string connectedAccountId)
    {
        BalanceService balanceService = new();

        RequestOptions requestOptions = new()
        {
            StripeAccount = connectedAccountId
        };

        return await balanceService.GetAsync(requestOptions);
    }
    #endregion
}

