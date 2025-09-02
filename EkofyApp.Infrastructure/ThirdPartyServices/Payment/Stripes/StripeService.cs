using Amazon.Runtime.Internal.Transform;
using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using CheckoutSession = Stripe.Checkout.Session;
using PortalSession = Stripe.BillingPortal.Session;
using PortalSessionService = Stripe.BillingPortal.SessionService;
using StripeInvoice = Stripe.Invoice;
using StripeSubscription = Stripe.Subscription;
using Subscription = EkofyApp.Domain.Entities.Subscription;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Payment.Stripes;
public sealed class StripeService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ILogger<StripeService> logger) : IStripeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<StripeService> _logger = logger;
    private readonly string WebhookSecretTestCustomer = Environment.GetEnvironmentVariable("STRIPE_SIGNATURE_SECRET_TEST_CUSTOMER") ?? throw new InvalidOperationException("STRIPE_SIGNATURE_SECRET_CUSTOMER is not configured.");

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

    public async Task<PortalSession> CreateCustomerPortalSessionAsync(string returnUrl)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string stripeCustomerId = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.StripeCustomerId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found any customer id with user id {userId}");

        PortalSessionService service = new();
        return service.Create(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,     // Customer đã tạo/lưu trong DB
            ReturnUrl = returnUrl      // URL quay về sau khi user xong việc
        }); // Link redirect user sang Customer Portal
    }

    // TODO: Nhớ gộp các params lại thành 1 object
    // Hiện tại chỉ đang test nên params đơn giản vậy
    // Và nhớ để Task void
    public async Task<PriceResponse> CreateSubscriptionPlanAsync(CreateSubScriptionPlanRequest createSubScriptionPlanRequest)
    {
        createSubScriptionPlanRequest.Metadata ??= [];
        if (createSubScriptionPlanRequest.Metadata.TryGetValue("name", out _))
        {
            throw new BadRequestCustomException("Metadata's key input must not contain 'name' key.");
        }
        createSubScriptionPlanRequest.Metadata.Add("name", createSubScriptionPlanRequest.Name);

        PriceService priceService = new();
        ProductService productService = new();

        StripeList<Price> existingPrices = priceService.List(new PriceListOptions
        {
            LookupKeys = [createSubScriptionPlanRequest.LookupKey],
            Limit = 1
        });

        // Kiểm tra nếu đã tồn tại Price với lookup_key này
        if (existingPrices.Data.Count > 0)
        {
            throw new ConflictCustomException("Price with the same lookup_key already exists.");
        }

        StripeSearchResult<Product> existingProducts = await productService.SearchAsync(new ProductSearchOptions
        {
            Query = $"active:'true' AND metadata['name']:'{createSubScriptionPlanRequest.Name}'",
            Limit = 1
        });
        if (existingProducts.Data.Count > 0)
        {
            throw new ConflictCustomException("Product with the same name already exists.");
        }

        // Tạo Product mới
        Product product = await productService.CreateAsync(new ProductCreateOptions
        {
            Active = true,
            Name = createSubScriptionPlanRequest.Name,
            // Tùy chọn thêm metadata và cách thay thế lookup_key
            Metadata = createSubScriptionPlanRequest.Metadata,
            Images = createSubScriptionPlanRequest.Images,
            Type = "service",
        });

        // Tạo Price với lookup_key
        Price price = await priceService.CreateAsync(new PriceCreateOptions
        {
            Active = true,
            UnitAmount = createSubScriptionPlanRequest.UnitAmount,
            Currency = CurrencyType.vnd.ToString(),
            Recurring = new PriceRecurringOptions
            {
                Interval = PeriodTime.month.ToString(),     // chu kỳ: month
                IntervalCount = createSubScriptionPlanRequest.IntervalCount,              // 1 tháng một lần
            },
            Product = product.Id,
            LookupKey = createSubScriptionPlanRequest.LookupKey,
            // Tùy chọn thêm metadata và cách thay thế lookup_key
            //Metadata = new Dictionary<string, string>
            //{
            //    { "plan_type", "premium_monthly" }
            //}
        });

        return new PriceResponse()
        {
            Id = price.Id,
            ProductId = price.ProductId,
            LookupKey = price.LookupKey,
            UnitAmount = price.UnitAmount ?? 0,
            Currency = price.Currency,
            Interval = price.Recurring!.Interval,
            IntervalCount = price.Recurring!.IntervalCount,
        };
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
    public async Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreateCheckoutSessionRequest createCheckoutSessionRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string customerId = _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.StripeCustomerId)
            .FirstOrDefault() ?? throw new NotFoundCustomException("Not found your Stripe Customer ID. Please contact us for more information.");

        // Lấy subscriptionId từ subscriptionTier và subscriptionVersion
        // Tạm thời chưa Lookup vì chưa hoàn thành Entity SubscriptionPlan
        // TODO: Cần lookup SubscriptionPlan để lấy StripePriceId
        string subscriptionId = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Tier == createCheckoutSessionRequest.SubscriptionTier && x.Version == createCheckoutSessionRequest.SubscriptionVersion)
            .Project(x => x.Id)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any subscription.");

        string stripePriceId = await _unitOfWork.GetCollection<SubscriptionPlan>()
            .Find(x => x.SubscriptionId == subscriptionId)
            .Project(x => x.StripePriceId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription plan's price.");

        SessionCreateOptions options = new()
        {
            PaymentMethodTypes = ["card", "link"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = stripePriceId, // ID gói đã tạo trong Stripes
                    Quantity = 1,
                },
            ],
            Customer = customerId, // có thể truyền customerId nếu đã có
            Mode = "payment",
            SuccessUrl = createCheckoutSessionRequest.SuccessUrl,
            CancelUrl = createCheckoutSessionRequest.CancelUrl,
        };

        SessionService service = new();
        var checkoutSession = service.Create(options);

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
    public async Task<CheckoutSessionResponse> CreateSubscriptionCheckoutSession(CreateCheckoutSessionRequest createCheckoutSessionRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        User user = _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.StripeCustomerId)
                .Include(x => x.Email))
            .FirstOrDefault() ?? throw new NotFoundCustomException("Not found your Stripe Customer ID. Please contact us for more information.");

        // Lấy subscriptionId từ subscriptionTier và subscriptionVersion
        // Tạm thời chưa Lookup vì chưa hoàn thành Entity SubscriptionPlan
        // TODO: Cần lookup SubscriptionPlan để lấy StripePriceId
        string subscriptionId = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Tier == createCheckoutSessionRequest.SubscriptionTier && x.Version == createCheckoutSessionRequest.SubscriptionVersion)
            .Project(x => x.Id)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any subscription.");

        string stripePriceId = await _unitOfWork.GetCollection<SubscriptionPlan>()
            .Find(x => x.SubscriptionId == subscriptionId)
            .Project(x => x.StripePriceId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription plan's price.");

        SessionCreateOptions options = new()
        {
            PaymentMethodTypes = ["card", "link"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = stripePriceId, // ID gói đã tạo trong Stripes
                    Quantity = 1,
                },
            ],
            Customer = user.StripeCustomerId, // có thể truyền customerId nếu đã có
            Mode = "subscription",
            //OriginContext = "web",
            SuccessUrl = createCheckoutSessionRequest.SuccessUrl,
            CancelUrl = createCheckoutSessionRequest.CancelUrl,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ReceiptEmail = createCheckoutSessionRequest.IsReceiptEmail ? user.Email : null, // Gửi biên lai về email của customer
                SetupFutureUsage = createCheckoutSessionRequest.IsSavePaymentMethod ? "off_session" : null, // Lưu thẻ để thanh toán các lần sau
            },
            //InvoiceCreation = new SessionInvoiceCreationOptions
            //{
            //    Enabled = true // Tạo hóa đơn cho lần thanh toán đầu tiên
            //},
        };

        SessionService service = new();
        CheckoutSession checkoutSession = service.Create(options);

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

    // TODO: Xử lý webhook từ Stripe cho Customer (tạm thời chỉ log ra)
    public void HandleWebhookCustomer(string json, string stripeSignature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTestCustomer);

            _logger.LogInformation($"Webhook event received: {stripeEvent.Type}");

            // Gọi hàm xử lý sự kiện tùy chỉnh
            if (stripeEvent.Type == EventTypes.CustomerCreated)
            {
                Customer customer = stripeEvent.Data.Object as Customer ?? throw new ArgumentNullCustomException("NULL");
                _logger.LogInformation($"Customer created: {customer.Id}, Email: {customer.Email}");
            }

            return;
        }
        catch (StripeException e)
        {
            _logger.LogError($"Webhook error: {e.Message}");
            return;
        }
    }

    // TODO: Xử lý webhook từ Stripe cho Express Connected Account (tạm thời chỉ log ra)
    public void HandleWebhookExpressConnectedAccount(string json, string stripeSignature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTestCustomer);

            _logger.LogInformation($"Webhook event received: {stripeEvent.Type}");

            if (stripeEvent.Type == EventTypes.AccountUpdated)
            {
                Account account = stripeEvent.Data.Object as Account ?? throw new ArgumentNullCustomException("NULL");
                _logger.LogInformation($"Account updated: {account.Id}, Email: {account.Email}, ChargesEnabled: {account.ChargesEnabled}");
            }
        }
        catch (StripeException e)
        {
            _logger.LogError($"Webhook error: {e.Message}");
        }
    }

    // Xử lý webhook từ Stripe
    // Thường sẽ lưu xuống database
    // TODO: Cần xử lý webhook để cập nhật subscription, thanh toán, v.v.
    public string HandleWebhook(string json, string stripeSignature, string webhookSecret)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

            switch (stripeEvent.Type)
            {
                case "invoice.paid":
                    var invoice = stripeEvent.Data.Object as StripeInvoice;
                    // Update DB: đánh dấu subscription đã trả thành công
                    return $"Invoice {invoice.Id} paid.";

                case "customer.subscription.deleted":
                    var subDeleted = stripeEvent.Data.Object as StripeSubscription;
                    // Update DB: hủy premium cho listener
                    return $"Subscription {subDeleted.Id} cancelled.";

                case "checkout.session.completed":
                    var session = stripeEvent.Data.Object as Session;
                    // Xử lý thanh toán 1 lần hoặc sub checkout
                    return $"Checkout completed for session {session.Id}.";

                default:
                    return $"Unhandled event type: {stripeEvent.Type}";
            }
        }
        catch (StripeException e)
        {
            return $"Webhook error: {e.Message}";
        }
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

