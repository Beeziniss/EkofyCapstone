using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
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
public sealed class StripeService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ILogger<StripeService> logger) : IStripeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
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
            .Find(x => x.Tier == createCheckoutSessionRequest.SubscriptionTier &&
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
            //InvoiceCreation = new SessionInvoiceCreationOptions
            //{
            //    Enabled = true, // Tạo hóa đơn
            //    //InvoiceData = new SessionInvoiceCreationInvoiceDataOptions
            //    //{
            //    //    Description = $"Invoice for {subscriptionPlan.Name} plan",
            //    //}
            //},
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

    // Chuyển tiền cho Artist
    // TODO: Cần cân nhắc hàm này được sử dụng thế nào, khi nào, ở đâu
    // Nên dùng void
    public TransferResponse TransferToArtist(string artistAccountId, long amount)
    {
        TransferService transferService = new();
        Transfer transfer = transferService.Create(new TransferCreateOptions
        {
            Amount = amount,
            Currency = CurrencyType.vnd.ToString(),
            Destination = artistAccountId,

            Description = "Royalty payout for streaming"
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
                Currency = CurrencyType.vnd.ToString(),
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
    public async Task<Payout> CreatePayoutAsync(string connectedAccountId, long amount, string? description = null, string currency = "sgd")
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
            Method = "standard", // Payout tiêu chuẩn (1-5 ngày làm việc)
            Description = description ?? alternativeDescription
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
            Method = "instant", // Payout tức thì (trong 30 phút, phí cao hơn)
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

    #region Refund Methods
    /// <summary>
    /// Tạo refund cho payment (full hoặc partial)
    /// </summary>
    public async Task<RefundResponse> CreateRefundAsync(CreateRefundRequest request)
    {
        string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Lấy payment transaction
        PaymentTransaction paymentTransaction = await _unitOfWork.GetCollection<PaymentTransaction>()
            .Find(x => x.Id == request.PaymentTransactionId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Payment transaction with ID {request.PaymentTransactionId} not found");

        // Kiểm tra payment đã được paid chưa
        if (paymentTransaction.PaymentStatus != PaymentTransactionStatus.Paid)
        {
            throw new BadRequestCustomException("Cannot refund a payment that is not paid");
        }

        // Kiểm tra amount refund
        decimal refundAmount;
        RefundType refundType;

        if (request.Amount.HasValue)
        {
            if (request.Amount.Value <= 0 || request.Amount.Value > paymentTransaction.Amount)
            {
                throw new BadRequestCustomException("Refund amount must be greater than 0 and not exceed the original payment amount");
            }
            refundAmount = request.Amount.Value;
            refundType = request.Amount.Value == paymentTransaction.Amount ? RefundType.Full : RefundType.Partial;
        }
        else
        {
            refundAmount = paymentTransaction.Amount;
            refundType = RefundType.Full;
        }

        // Kiểm tra đã có refund cho payment này chưa
        var existingRefunds = await _unitOfWork.GetCollection<RefundTransaction>()
            .Find(x => x.PaymentTransactionId == request.PaymentTransactionId && 
                      (x.Status == RefundTransactionStatus.Succeeded || x.Status == RefundTransactionStatus.Pending))
            .ToListAsync();

        decimal totalRefundedAmount = existingRefunds.Sum(r => r.Amount);
        if (totalRefundedAmount + refundAmount > paymentTransaction.Amount)
        {
            throw new BadRequestCustomException($"Total refund amount ({totalRefundedAmount + refundAmount}) cannot exceed original payment amount ({paymentTransaction.Amount})");
        }

        // Tạo refund với Stripe
        var refundService = new RefundService();
        var refundOptions = new RefundCreateOptions
        {
            PaymentIntent = paymentTransaction.StripePaymentId,
            Amount = (long)(refundAmount * 100), // Convert to cents
            Reason = request.Reason,
            Metadata = request.Metadata,
            ReverseTransfer = request.ReverseTransfer
        };

        Refund stripeRefund = await refundService.CreateAsync(refundOptions);

        // Lưu refund transaction vào database
        var refundTransaction = new RefundTransaction
        {
            UserId = paymentTransaction.UserId,
            PaymentTransactionId = request.PaymentTransactionId,
            StripeRefundId = stripeRefund.Id,
            StripePaymentIntentId = paymentTransaction.StripePaymentId!,
            StripeChargeId = stripeRefund.ChargeId,
            Amount = refundAmount,
            Currency = paymentTransaction.Currency,
            Type = refundType,
            Status = Enum.Parse<RefundTransactionStatus>(stripeRefund.Status, true),
            Reason = request.Reason,
            Description = request.Description,
            Metadata = request.Metadata,
            ProcessedByUserId = currentUserId,
            ProcessedAt = HelperMethod.GetUtcPlus7TimeOffset()
        };

        await _unitOfWork.GetCollection<RefundTransaction>().InsertOneAsync(refundTransaction);

        _logger.LogInformation("Refund created: {RefundId} for Payment: {PaymentId}, Amount: {Amount} {Currency}", 
            stripeRefund.Id, paymentTransaction.StripePaymentId, refundAmount, paymentTransaction.Currency);

        return new RefundResponse
        {
            Id = refundTransaction.Id,
            StripeRefundId = stripeRefund.Id,
            PaymentTransactionId = request.PaymentTransactionId,
            StripePaymentIntentId = paymentTransaction.StripePaymentId!,
            StripeChargeId = stripeRefund.ChargeId,
            Amount = refundAmount,
            Currency = paymentTransaction.Currency,
            Type = refundType,
            Status = refundTransaction.Status,
            Reason = request.Reason,
            Description = request.Description,
            Metadata = request.Metadata,
            ProcessedByUserId = currentUserId,
            ProcessedAt = refundTransaction.ProcessedAt,
            CreatedAt = refundTransaction.CreatedAt,
            UpdatedAt = refundTransaction.UpdatedAt
        };
    }

    /// <summary>
    /// Lấy thông tin refund theo ID
    /// </summary>
    public async Task<RefundResponse?> GetRefundAsync(string refundTransactionId)
    {
        RefundTransaction? refundTransaction = await _unitOfWork.GetCollection<RefundTransaction>()
            .Find(x => x.Id == refundTransactionId)
            .FirstOrDefaultAsync();

        if (refundTransaction == null)
            return null;

        return new RefundResponse
        {
            Id = refundTransaction.Id,
            StripeRefundId = refundTransaction.StripeRefundId,
            PaymentTransactionId = refundTransaction.PaymentTransactionId,
            StripePaymentIntentId = refundTransaction.StripePaymentIntentId,
            StripeChargeId = refundTransaction.StripeChargeId,
            Amount = refundTransaction.Amount,
            Currency = refundTransaction.Currency,
            Type = refundTransaction.Type,
            Status = refundTransaction.Status,
            Reason = refundTransaction.Reason,
            Description = refundTransaction.Description,
            Metadata = refundTransaction.Metadata,
            ProcessedByUserId = refundTransaction.ProcessedByUserId,
            ProcessedAt = refundTransaction.ProcessedAt,
            CreatedAt = refundTransaction.CreatedAt,
            UpdatedAt = refundTransaction.UpdatedAt
        };
    }

    /// <summary>
    /// Lấy danh sách refunds
    /// </summary>
    public async Task<List<RefundResponse>> ListRefundsAsync(string? paymentTransactionId = null, int limit = 10, string? startingAfter = null)
    {
        var filterBuilder = Builders<RefundTransaction>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(paymentTransactionId))
        {
            filter &= filterBuilder.Eq(x => x.PaymentTransactionId, paymentTransactionId);
        }

        if (!string.IsNullOrEmpty(startingAfter))
        {
            filter &= filterBuilder.Lt(x => x.Id, startingAfter);
        }

        var refunds = await _unitOfWork.GetCollection<RefundTransaction>()
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        return refunds.Select(r => new RefundResponse
        {
            Id = r.Id,
            StripeRefundId = r.StripeRefundId,
            PaymentTransactionId = r.PaymentTransactionId,
            StripePaymentIntentId = r.StripePaymentIntentId,
            StripeChargeId = r.StripeChargeId,
            Amount = r.Amount,
            Currency = r.Currency,
            Type = r.Type,
            Status = r.Status,
            Reason = r.Reason,
            Description = r.Description,
            Metadata = r.Metadata,
            ProcessedByUserId = r.ProcessedByUserId,
            ProcessedAt = r.ProcessedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }
    #endregion

    #region Escrow Payment Methods (Split Payment)
    /// <summary>
    /// Tạo checkout session cho escrow payment với split payment configuration
    /// </summary>
    public async Task<CheckoutSessionResponse> CreateEscrowPaymentCheckoutSessionAsync(CreateEscrowPaymentRequest request)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Lấy thông tin user và artist package
        User user = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.StripeCustomerId)
                .Include(x => x.Email)
                .Include(x => x.FullName))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("User not found");

        ArtistPackage artistPackage = await _unitOfWork.GetCollection<ArtistPackage>()
            .Find(x => x.Id == request.ArtistPackageId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Artist package not found");

        // Lấy thông tin artist và kiểm tra connected account
        User artist = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == artistPackage.ArtistId)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.StripeAccountId)
                .Include(x => x.FullName))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Artist not found");

        if (string.IsNullOrEmpty(artist.StripeAccountId))
        {
            throw new BadRequestCustomException("Artist must have a connected Stripe account to receive payments");
        }

        // Tính toán split percentages
        decimal advancePercentage = request.AdvancePaymentPercentage ?? 30m;
        decimal completionPercentage = request.CompletionPaymentPercentage ?? 60m;
        decimal platformCommissionPercentage = request.PlatformCommissionPercentage ?? 10m;

        // Validate percentages
        if (advancePercentage + completionPercentage + platformCommissionPercentage != 100m)
        {
            throw new BadRequestCustomException("Payment split percentages must total 100%");
        }

        decimal totalAmount = artistPackage.Amount;
        decimal advanceAmount = Math.Round(totalAmount * advancePercentage / 100, 2);
        decimal completionAmount = Math.Round(totalAmount * completionPercentage / 100, 2);
        decimal platformCommission = Math.Round(totalAmount * platformCommissionPercentage / 100, 2);

        // Tạo checkout session với metadata cho escrow
        var sessionOptions = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = artistPackage.Currency.ToString(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Escrow Payment: {artistPackage.PackageName}",
                            Description = $"Split payment - Advance: {advancePercentage}%, Completion: {completionPercentage}%, Platform: {platformCommissionPercentage}%",
                        },
                        UnitAmountDecimal = totalAmount * 100, // Convert to cents
                    },
                    Quantity = 1,
                },
            },
            Customer = user.StripeCustomerId,
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                CaptureMethod = "automatic", // Capture immediately but don't transfer yet
                SetupFutureUsage = request.IsSavePaymentMethod ? "off_session" : null,
                ReceiptEmail = request.IsReceiptEmail ? user.Email : null,
            },
            Metadata = new Dictionary<string, string>
            {
                { "is_escrow", "true" },
                { "artist_package_id", request.ArtistPackageId },
                { "artist_id", artistPackage.ArtistId },
                { "buyer_id", userId },
                { "advance_percentage", advancePercentage.ToString() },
                { "completion_percentage", completionPercentage.ToString() },
                { "platform_commission_percentage", platformCommissionPercentage.ToString() },
                { "advance_amount", advanceAmount.ToString() },
                { "completion_amount", completionAmount.ToString() },
                { "platform_commission", platformCommission.ToString() },
                { "artist_stripe_account", artist.StripeAccountId },
                { "estimated_delivery_days", request.EstimatedDeliveryDays.ToString() },
                { "max_revisions", request.MaxRevisions.ToString() }
            }
        };

        var sessionService = new SessionService();
        Session checkoutSession = await sessionService.CreateAsync(sessionOptions);

        if (string.IsNullOrEmpty(checkoutSession.Url))
        {
            throw new NotFoundCustomException("Error while generating URL for checkout session");
        }

        // Tạo payment transaction record cho escrow
        await _unitOfWork.GetCollection<PaymentTransaction>().InsertOneAsync(new PaymentTransaction
        {
            UserId = userId,
            StripeCheckoutSessionId = checkoutSession.Id,
            StripePaymentId = checkoutSession.PaymentIntentId,
            StripePaymentMethod = checkoutSession.PaymentMethodTypes,
            Amount = totalAmount,
            Currency = artistPackage.Currency.ToString(),
            PaymentStatus = PaymentTransactionStatus.Pending,
            Status = TransactionStatus.Open
        });

        _logger.LogInformation("Escrow payment checkout session created: {SessionId} for Artist Package: {PackageId}", 
            checkoutSession.Id, request.ArtistPackageId);

        return new CheckoutSessionResponse
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

    /// <summary>
    /// Lấy thông tin escrow payment
    /// </summary>
    public async Task<EscrowPaymentResponse> GetEscrowPaymentAsync(string orderId)
    {
        ArtistPackageOrder order = await _unitOfWork.GetCollection<ArtistPackageOrder>()
            .Find(x => x.Id == orderId && x.IsEscrowPayment)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Escrow order not found");

        if (order.EscrowPayment == null)
        {
            throw new BadRequestCustomException("Order does not have escrow payment configuration");
        }

        return new EscrowPaymentResponse
        {
            Id = order.Id,
            OrderId = order.Id,
            PaymentTransactionId = order.PaymentTransactionId,
            StripePaymentIntentId = order.EscrowPayment.StripePaymentIntentId,
            TotalAmount = order.EscrowPayment.TotalAmount,
            AdvancePaymentAmount = order.EscrowPayment.AdvancePaymentAmount,
            CompletionPaymentAmount = order.EscrowPayment.CompletionPaymentAmount,
            PlatformCommissionAmount = order.EscrowPayment.PlatformCommissionAmount,
            Currency = order.EscrowPayment.Currency,
            Status = order.EscrowPayment.Status,
            OrderStatus = order.Status,
            AdvancePaymentReleasedAt = order.EscrowPayment.AdvancePaymentReleasedAt,
            OrderCompletedAt = order.EscrowPayment.OrderCompletedAt,
            AutoReleaseDate = order.EscrowPayment.AutoReleaseDate,
            BuyerId = order.ClientId,
            ArtistId = order.ProviderId,
            ArtistPackageId = order.ArtistPackageId,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    /// <summary>
    /// Lấy danh sách escrow payments
    /// </summary>
    public async Task<List<EscrowPaymentResponse>> ListEscrowPaymentsAsync(string? userId = null, int limit = 10)
    {
        var filterBuilder = Builders<ArtistPackageOrder>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Ne(x => x.EscrowPayment, null), // Only escrow orders
            filterBuilder.Eq(x => x.IsEscrowPayment, true)
        );

        if (!string.IsNullOrEmpty(userId))
        {
            filter &= filterBuilder.Or(
                filterBuilder.Eq(x => x.ClientId, userId),
                filterBuilder.Eq(x => x.ProviderId, userId)
            );
        }

        var orders = await _unitOfWork.GetCollection<ArtistPackageOrder>()
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        return orders.Select(order => new EscrowPaymentResponse
        {
            Id = order.Id,
            OrderId = order.Id,
            PaymentTransactionId = order.PaymentTransactionId,
            StripePaymentIntentId = order.EscrowPayment?.StripePaymentIntentId ?? "",
            TotalAmount = order.EscrowPayment?.TotalAmount ?? 0,
            AdvancePaymentAmount = order.EscrowPayment?.AdvancePaymentAmount ?? 0,
            CompletionPaymentAmount = order.EscrowPayment?.CompletionPaymentAmount ?? 0,
            PlatformCommissionAmount = order.EscrowPayment?.PlatformCommissionAmount ?? 0,
            Currency = order.EscrowPayment?.Currency ?? "usd",
            Status = order.EscrowPayment?.Status ?? EscrowTransactionStatus.Pending,
            OrderStatus = order.Status,
            AdvancePaymentReleasedAt = order.EscrowPayment?.AdvancePaymentReleasedAt,
            OrderCompletedAt = order.EscrowPayment?.OrderCompletedAt,
            AutoReleaseDate = order.EscrowPayment?.AutoReleaseDate,
            BuyerId = order.ClientId,
            ArtistId = order.ProviderId,
            ArtistPackageId = order.ArtistPackageId,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        }).ToList();
    }

    /// <summary>
    /// Release advance payment (30%) cho artist khi order được confirm
    /// </summary>
    public async Task<EscrowPaymentResponse> ReleaseAdvancePaymentAsync(string orderId)
    {
        ArtistPackageOrder order = await _unitOfWork.GetCollection<ArtistPackageOrder>()
            .Find(x => x.Id == orderId && x.IsEscrowPayment)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Escrow order not found");

        if (order.EscrowPayment == null)
        {
            throw new BadRequestCustomException("Order does not have escrow payment configuration");
        }

        if (order.EscrowPayment.Status != EscrowTransactionStatus.Pending)
        {
            throw new BadRequestCustomException("Advance payment can only be released for pending escrow transactions");
        }

        if (order.EscrowPayment.AdvancePaymentReleasedAt.HasValue)
        {
            throw new BadRequestCustomException("Advance payment has already been released");
        }

        // Tạo transfer cho advance payment
        var transferService = new TransferService();
        var transferOptions = new TransferCreateOptions
        {
            Amount = (long)(order.EscrowPayment.AdvancePaymentAmount * 100), // Convert to cents
            Currency = order.EscrowPayment.Currency,
            Destination = order.EscrowPayment.ArtistStripeAccountId,
            Description = $"Advance payment for order - {order.EscrowPayment.AdvancePaymentPercentage}%",
            Metadata = new Dictionary<string, string>
            {
                { "order_id", orderId },
                { "payment_type", "advance" },
                { "artist_id", order.ProviderId },
                { "buyer_id", order.ClientId }
            }
        };

        Transfer transfer = await transferService.CreateAsync(transferOptions);

        // Cập nhật order với advance payment info
        var updateDefinition = Builders<ArtistPackageOrder>.Update
            .Set(x => x.EscrowPayment.StripeAdvanceTransferId, transfer.Id)
            .Set(x => x.EscrowPayment.AdvancePaymentReleasedAt, HelperMethod.GetUtcPlus7TimeOffset())
            .Set(x => x.EscrowPayment.Status, EscrowTransactionStatus.PartialReleased)
            .Set(x => x.Status, ArtistPackageOrderStatus.InProgress)
            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        await _unitOfWork.GetCollection<ArtistPackageOrder>()
            .UpdateOneAsync(x => x.Id == orderId, updateDefinition);

        _logger.LogInformation("Advance payment released: {TransferId} for Order: {OrderId}, Amount: {Amount} {Currency}", 
            transfer.Id, orderId, order.EscrowPayment.AdvancePaymentAmount, order.EscrowPayment.Currency);

        return await GetEscrowPaymentAsync(orderId);
    }

    /// <summary>
    /// Release completion payment (60%) cho artist khi order hoàn thành
    /// </summary>
    public async Task<EscrowPaymentResponse> ReleaseCompletionPaymentAsync(string orderId)
    {
        ArtistPackageOrder order = await _unitOfWork.GetCollection<ArtistPackageOrder>()
            .Find(x => x.Id == orderId && x.IsEscrowPayment)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Escrow order not found");

        if (order.EscrowPayment == null)
        {
            throw new BadRequestCustomException("Order does not have escrow payment configuration");
        }

        if (order.EscrowPayment.Status != EscrowTransactionStatus.PartialReleased)
        {
            throw new BadRequestCustomException("Completion payment can only be released after advance payment");
        }

        if (order.EscrowPayment.CompletionPaymentReleasedAt.HasValue)
        {
            throw new BadRequestCustomException("Completion payment has already been released");
        }

        // Tạo transfer cho completion payment
        var transferService = new TransferService();
        var transferOptions = new TransferCreateOptions
        {
            Amount = (long)(order.EscrowPayment.CompletionPaymentAmount * 100), // Convert to cents
            Currency = order.EscrowPayment.Currency,
            Destination = order.EscrowPayment.ArtistStripeAccountId,
            Description = $"Completion payment for order - {order.EscrowPayment.CompletionPaymentPercentage}%",
            Metadata = new Dictionary<string, string>
            {
                { "order_id", orderId },
                { "payment_type", "completion" },
                { "artist_id", order.ProviderId },
                { "buyer_id", order.ClientId }
            }
        };

        Transfer transfer = await transferService.CreateAsync(transferOptions);

        // Cập nhật order với completion payment info
        var updateDefinition = Builders<ArtistPackageOrder>.Update
            .Set(x => x.EscrowPayment.StripeCompletionTransferId, transfer.Id)
            .Set(x => x.EscrowPayment.CompletionPaymentReleasedAt, HelperMethod.GetUtcPlus7TimeOffset())
            .Set(x => x.EscrowPayment.OrderCompletedAt, HelperMethod.GetUtcPlus7TimeOffset())
            .Set(x => x.EscrowPayment.Status, EscrowTransactionStatus.Completed)
            .Set(x => x.Status, ArtistPackageOrderStatus.Completed)
            .Set(x => x.CompletedAt, HelperMethod.GetUtcPlus7TimeOffset())
            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        await _unitOfWork.GetCollection<ArtistPackageOrder>()
            .UpdateOneAsync(x => x.Id == orderId, updateDefinition);

        _logger.LogInformation("Completion payment released: {TransferId} for Order: {OrderId}, Amount: {Amount} {Currency}", 
            transfer.Id, orderId, order.EscrowPayment.CompletionPaymentAmount, order.EscrowPayment.Currency);

        return await GetEscrowPaymentAsync(orderId);
    }

    /// <summary>
    /// Confirm order completion bởi buyer - trigger release completion payment
    /// </summary>
    public async Task<EscrowPaymentResponse> ConfirmOrderCompletionAsync(ConfirmOrderCompletionRequest request)
    {
        string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        ArtistPackageOrder order = await _unitOfWork.GetCollection<ArtistPackageOrder>()
            .Find(x => x.Id == request.OrderId && x.IsEscrowPayment)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Escrow order not found");

        // Chỉ buyer mới có thể confirm completion
        if (order.ClientId != currentUserId)
        {
            throw new ForbiddenCustomException("Only the buyer can confirm order completion");
        }

        if (order.Status != ArtistPackageOrderStatus.SubmittedForReview)
        {
            throw new BadRequestCustomException("Order must be submitted for review before completion can be confirmed");
        }

        // Cập nhật order với delivery files và review
        var orderUpdateDefinition = Builders<ArtistPackageOrder>.Update
            .Set(x => x.Status, ArtistPackageOrderStatus.Completed)
            .Set(x => x.CompletedAt, HelperMethod.GetUtcPlus7TimeOffset())
            .Set(x => x.ReviewedAt, HelperMethod.GetUtcPlus7TimeOffset())
            .Set(x => x.ClientRating, request.BuyerRating)
            .Set(x => x.ClientReview, request.BuyerReview)
            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        await _unitOfWork.GetCollection<ArtistPackageOrder>()
            .UpdateOneAsync(x => x.Id == order.Id, orderUpdateDefinition);

        // Tự động release completion payment
        return await ReleaseCompletionPaymentAsync(order.Id);
    }
    #endregion
}

