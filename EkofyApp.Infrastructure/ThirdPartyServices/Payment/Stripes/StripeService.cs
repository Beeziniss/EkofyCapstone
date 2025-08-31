using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Stripe;
using Stripe.Checkout;
using CheckoutSession = Stripe.Checkout.Session;
using PortalSession = Stripe.BillingPortal.Session;
using PortalSessionService = Stripe.BillingPortal.SessionService;
using StripeInvoice = Stripe.Invoice;
using StripeSubscription = Stripe.Subscription;
using Subscription = EkofyApp.Domain.Entities.Subscription;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Payment.Stripes;
public sealed class StripeService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IStripeService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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
    public async Task<Account> CreateExpressConnectedAccount()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string email = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.Email)
            .FirstOrDefaultAsync();

        AccountService accountService = new();
        return accountService.Create(new AccountCreateOptions
        {
            Type = "express",  // Express phổ biến nhất
            Country = "SG",   // Sandbox test US/EU (VN không hỗ trợ)
            Email = email
        });
    }

    // Tạo link onboarding để Artist nhập thông tin
    public AccountLink CreateAccountOnboardingLink(string refreshUrl, string returnUrl)
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

    // Tạo Customer
    // TODO: Lưu customerId vào DB User nhưng chỉ khi user thanh toán thành công lần đầu
    public async Task<Customer> CreateCustomerAsync(string? name)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string email = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.Email)
            .FirstOrDefaultAsync();

        CustomerService customerService = new();
        return customerService.Create(new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            //PaymentMethod = "pm_card_visa", // test tạm
        });
    }

    public async Task<PortalSession> CreateCustomerPortalSessionAsync(string returnUrl)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string stripeCustomerId = await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == userId)
            .Project(x => x.StripeCustomerId)
            .FirstOrDefaultAsync();

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
    public async Task<Price> CreateSubscriptionPlan(string lookupKey, string subscriptionPlanName, long unitAmount, long intervalCount = 1, List<string>? images = null, Dictionary<string, string>? metadata = null)
    {
        metadata ??= [];
        if (metadata.TryGetValue("name", out _))
        {
            throw new BadRequestCustomException("Metadata's key input must not contain 'name' key.");
        }
        metadata.Add("name", subscriptionPlanName);

        PriceService priceService = new();
        ProductService productService = new();

        StripeList<Price> existingPrices = priceService.List(new PriceListOptions
        {
            LookupKeys = [lookupKey],
            Limit = 1
        });

        // Kiểm tra nếu đã tồn tại Price với lookup_key này
        if (existingPrices.Data.Count > 0)
        {
            throw new ConflictCustomException("Price with the same lookup_key already exists.");
        }

        StripeSearchResult<Product> existingProducts = await productService.SearchAsync(new ProductSearchOptions
        {
            Query = $"active:'true' AND metadata['name']:'{subscriptionPlanName}'",
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
            Name = subscriptionPlanName,
            // Tùy chọn thêm metadata và cách thay thế lookup_key
            Metadata = metadata,
            Images = images,
            Type = "service",
        });

        // Tạo Price với lookup_key
        Price price = await priceService.CreateAsync(new PriceCreateOptions
        {
            Active = true,
            UnitAmount = unitAmount,
            Currency = CurrencyType.vnd.ToString(),
            Recurring = new PriceRecurringOptions
            {
                Interval = PeriodTime.month.ToString(),     // chu kỳ: month
                IntervalCount = intervalCount,              // 1 tháng một lần
            },
            Product = product.Id,
            LookupKey = lookupKey,
            // Tùy chọn thêm metadata và cách thay thế lookup_key
            //Metadata = new Dictionary<string, string>
            //{
            //    { "plan_type", "premium_monthly" }
            //}
        });

        return price;
    }

    // Tạo Checkout Session (link) cho thanh toán 1 lần
    public async Task<CheckoutSession> CreatePaymentCheckoutSessionAsync(SubscriptionTier subscriptionTier, int subscriptionVersion, string successUrl, string cancelUrl)
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
            .Find(x => x.Tier == subscriptionTier && x.Version == subscriptionVersion)
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
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        };

        SessionService service = new();
        return service.Create(options); // session.Url chính là link bạn gửi cho user
    }

    // Tạo Checkout Session (link) cho đăng ký gói (subscription)
    public async Task<CheckoutSession> CreateSubscriptionCheckoutSession(SubscriptionTier subscriptionTier, int subscriptionVersion, string successUrl, string cancelUrl)
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
            .Find(x => x.Tier == subscriptionTier && x.Version == subscriptionVersion)
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
            Mode = "subscription",
            //OriginContext = "web",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        };

        SessionService service = new();
        return service.Create(options); // session.Url chính là link bạn gửi cho user
    }

    // Chuyển tiền cho Artist
    // TODO: Cần cân nhắc hàm này được sử dụng thế nào, khi nào, ở đâu
    // Nên dùng void
    public Transfer TransferToArtist(long amount, string artistAccountId, string currency = "vnd")
    {
        var transferService = new TransferService();
        var transfer = transferService.Create(new TransferCreateOptions
        {
            Amount = amount,  // tính theo cents
            Currency = currency,
            Destination = artistAccountId,

            Description = "Royalty payout for streaming"
        });
        return transfer;
    }

    // Chuyển tiền cho nhiều Artist
    // TODO: Cần cân nhắc hàm này được sử dụng thế nào, khi nào, ở đâu
    // Nên dùng void
    public List<Transfer> TransferGroupArtist(string[] artistAccountIds, long amount, string currency = "vnd", string groupId = "default")
    {
        List<Transfer> transfers = [];
        TransferService transferService = new();
        foreach (string artistAccountId in artistAccountIds)
        {
            Transfer transfer = transferService.Create(new TransferCreateOptions
            {
                Amount = amount,
                Currency = currency,
                Destination = artistAccountId,
                TransferGroup = groupId,
                Description = "Royalty payout for streaming"
            });

            transfers.Add(transfer);
        }

        return transfers;
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

