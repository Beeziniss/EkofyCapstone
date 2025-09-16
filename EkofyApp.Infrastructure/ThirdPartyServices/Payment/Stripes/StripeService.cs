using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Coupons;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Exceptions;
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
                        //Interval = "manual" // Chuyển tiền thủ công
                        Interval = "monthly", // Tự động chuyển tiền hàng tháng,
                        DelayDays = 3, // sau 3 ngày
                        MonthlyPayoutDays = [28, 31], // vào ngày 28 và 31 hàng tháng
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
                        //Interval = "manual" // Rút tiền thủ công
                        Interval = "monthly", // Tự động rút tiền hàng tháng,
                        DelayDays = 3, // sau 3 ngày
                        MonthlyPayoutDays = [28, 31], // vào ngày 28 và 31 hàng tháng
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
            throw new NotFoundCustomException("Nothing is updated.");
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
    // Dùng subscription checkout session thay thế
    //public async Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreateCheckoutSessionRequest createCheckoutSessionRequest)
    //{
    //    string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

    //    User user = _unitOfWork.GetCollection<User>()
    //        .Find(x => x.Id == userId)
    //        .Project<User>(Builders<User>.Projection
    //            .Include(x => x.StripeCustomerId)
    //            .Include(x => x.Email))
    //        .FirstOrDefault() ?? throw new NotFoundCustomException("Not found your Stripe Customer ID. Please contact us for more information.");

    //    // Lấy subscriptionId từ subscriptionTier và subscriptionVersion
    //    // Tạm thời chưa Lookup vì chưa hoàn thành Entity SubscriptionPlan
    //    // TODO: Cần lookup SubscriptionPlan để lấy StripePriceId
    //    string subscriptionId = await _unitOfWork.GetCollection<Subscription>()
    //        .Find(x => x.Tier == createCheckoutSessionRequest.SubscriptionTier &&
    //            x.Version == createCheckoutSessionRequest.SubscriptionVersion &&
    //            x.Status == SubscriptionStatus.Active)
    //        .Project(x => x.Id)
    //        .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any subscription.");

    //    SubscriptionPlan subscriptionPlan = await _unitOfWork.GetCollection<SubscriptionPlan>()
    //        .Find(x => x.SubscriptionId == subscriptionId)
    //        .Project<SubscriptionPlan>(Builders<SubscriptionPlan>.Projection
    //            .Include(x => x.Id)
    //            .ElemMatch(x => x.SubscriptionPlanPrices, p => p.Interval == createCheckoutSessionRequest.Period))
    //        .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription plan's price.");

    //    // Lấy coupon giảm giá nếu có
    //    // TODO: Cần sửa lại nếu không phải yearly thì không lấy coupon
    //    List<string> couponIds = [];
    //    if (createCheckoutSessionRequest.Period == PeriodTime.year)
    //    {
    //        //couponIds = await _unitOfWork.GetCollection<EntityCoupon>()
    //        //    .Find(x => createCheckoutSessionRequest.CouponCodes.Contains(x.Code) && x.Status == CouponStatus.Active)
    //        //    .Project(x => x.StripeCouponId)
    //        //    .ToListAsync();

    //        couponIds = ["npw1701t"];
    //    }

    //    CheckoutOption.SessionCreateOptions options = new()
    //    {
    //        PaymentMethodTypes = ["card", "link"],
    //        LineItems =
    //        [
    //            new SessionLineItemOptions
    //            {
    //                Price = subscriptionPlan.SubscriptionPlanPrices.First().StripePriceId, // ID gói đã tạo trong Stripes
    //                Quantity = 1,
    //            },
    //        ],
    //        Customer = user.StripeCustomerId, // có thể truyền customerId nếu đã có
    //        Mode = "payment",
    //        SuccessUrl = createCheckoutSessionRequest.SuccessUrl,
    //        CancelUrl = createCheckoutSessionRequest.CancelUrl,
    //        PaymentIntentData = new SessionPaymentIntentDataOptions
    //        {
    //            ReceiptEmail = createCheckoutSessionRequest.IsReceiptEmail ? user.Email : null, // Gửi biên lai về email của customer
    //            SetupFutureUsage = createCheckoutSessionRequest.IsSavePaymentMethod ? "off_session" : null, // Lưu thẻ để thanh toán các lần sau
    //        },
    //        //InvoiceCreation = new SessionInvoiceCreationOptions
    //        //{
    //        //    Enabled = true // Tạo hóa đơn cho thanh toán
    //        //},
    //        Discounts = couponIds.Select(x => new SessionDiscountOptions
    //        {
    //            Coupon = x
    //        }).ToList(),
    //    };

    //    CheckoutOption.SessionService service = new();
    //    CheckoutOption.Session checkoutSession = service.Create(options);
    //    if (string.IsNullOrEmpty(checkoutSession.Url))
    //    {
    //        throw new NotFoundCustomException("Error while generating URL for checkout session");
    //    }

    //    await _unitOfWork.GetCollection<Transaction>().InsertOneAsync(new Transaction
    //    {
    //        UserId = userId,
    //        SubscriptionId = subscriptionId,
    //        SubscriptionPlanId = subscriptionPlan.Id,
    //        StripeCheckoutSessionId = checkoutSession.Id,
    //        StripePaymentId = checkoutSession.PaymentIntentId, // Lúc này chưa có paymentId nên null
    //        StripePaymentMethod = checkoutSession.PaymentMethodTypes,
    //        Amount = Convert.ToDecimal(checkoutSession.AmountTotal),
    //        Currency = checkoutSession.Currency,

    //        PaymentStatus = PaymentStatus.Pending,
    //        Status = TransactionStatus.Open
    //    });

    //    return new()
    //    {
    //        Id = checkoutSession.Id,
    //        Url = checkoutSession.Url,
    //        SuccessUrl = checkoutSession.SuccessUrl,
    //        CancelUrl = checkoutSession.CancelUrl,
    //        Status = checkoutSession.Status,
    //        Mode = checkoutSession.Mode,
    //        Created = checkoutSession.Created,
    //        Expired = checkoutSession.ExpiresAt,
    //    };
    //}

    // Tạo Checkout Session (link) cho đăng ký gói (subscription)

    public async Task<CheckoutSessionResponse> CreateSubscriptionCheckoutSession(CreateCheckoutSessionRequest createCheckoutSessionRequest)
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
                .Include(x => x.Id))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any subscription.");

        SubscriptionPlan subscriptionPlan = await _unitOfWork.GetCollection<SubscriptionPlan>()
            .Find(x => x.SubscriptionId == subscription.Id && x.StripeProductActive == true)
            .Project<SubscriptionPlan>(Builders<SubscriptionPlan>.Projection
                .Include(x => x.Id)
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
        };

        CheckoutOption.SessionService service = new();
        CheckoutOption.Session checkoutSession = service.Create(options);
        if (string.IsNullOrEmpty(checkoutSession.Url))
        {
            throw new NotFoundCustomException("Error while generating URL for checkout session");
        }

        await _unitOfWork.GetCollection<Transaction>().InsertOneAsync(new Transaction
        {
            UserId = userId,
            SubscriptionId = subscription.Id,
            SubscriptionPlanId = subscriptionPlan.Id,
            StripeCheckoutSessionId = checkoutSession.Id,
            StripePaymentId = checkoutSession.PaymentIntentId, // Lúc này chưa có paymentId nên null
            StripePaymentMethod = checkoutSession.PaymentMethodTypes,
            Amount = Convert.ToDecimal(checkoutSession.AmountTotal),
            Currency = checkoutSession.Currency,

            PaymentStatus = PaymentStatus.Pending,
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
}

