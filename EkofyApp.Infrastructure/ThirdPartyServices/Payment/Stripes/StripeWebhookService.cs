using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Stripe;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Payment.Stripes;
public sealed class StripeWebhookService(IUnitOfWork unitOfWork, ILogger<StripeService> logger, IUserSubscriptionService userSubscriptionService, IEffectiveEntitlementService effectiveEntitlementService) : IStripeWebhookService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<StripeService> _logger = logger;
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;
    private readonly IEffectiveEntitlementService _effectiveEntitlementService = effectiveEntitlementService;
    private readonly string WebhookSecretTest = Environment.GetEnvironmentVariable("STRIPE_SIGNATURE_SECRET_TEST") ?? throw new InvalidOperationException("STRIPE_SIGNATURE_SECRET_TEST is not configured.");

    // TODO: Xử lý webhook từ Stripe cho Customer (tạm thời chỉ log ra)
    public async Task HandleWebhookCustomerAsync(string json, string stripeSignature)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            try
            {
                Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTest);

                // Gọi hàm xử lý sự kiện tùy chỉnh
                switch (stripeEvent.Type)
                {
                    case EventTypes.CustomerCreated:
                        {
                            Customer customer = stripeEvent.Data.Object as Customer ?? throw new ArgumentNullCustomException("Customer is NULL");

                            await _unitOfWork.GetCollection<User>().UpdateOneAsync(session, x => x.Email == customer.Email,
                                Builders<User>.Update
                                .Set(x => x.StripeCustomerId, customer.Id)
                                .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                            );
                            break;
                        }

                    case EventTypes.CustomerSubscriptionCreated: // Đăng ký currentSubscription mới của customer (bao gồm cả lần đầu)
                        {
                            StripeSubscription stripeSubscription = stripeEvent.Data.Object as StripeSubscription ?? throw new ArgumentNullCustomException("Subscription is NULL");

                            // Lấy metadata từ stripeSubscription
                            // Sau đó xử lý cấp quyền entitlements cho user
                            // Tính toán lại PeriodEnd
                            DateTimeOffset periodEnd = stripeSubscription.Metadata["interval"] switch
                            {
                                "day" => HelperMethod.GetUtcPlus7TimeOffset().AddDays(1),
                                "week" => HelperMethod.GetUtcPlus7TimeOffset().AddDays(7),
                                "month" => HelperMethod.GetUtcPlus7TimeOffset().AddMonths(1),
                                "year" => HelperMethod.GetUtcPlus7TimeOffset().AddYears(1),
                                _ => throw new BadRequestCustomException("Interval is not supported.")
                            };

                            // Cập nhật status UserSubscription thành Inactive/Deprecated
                            await _unitOfWork.GetCollection<UserSubscription>().UpdateOneAsync(session, x => x.UserId == stripeSubscription.Metadata["user_id"] && x.IsActive == true,
                                Builders<UserSubscription>.Update
                                .Set(x => x.IsActive, false)
                                .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                            );

                            // Tạo mới UserSubscription mỗi lần có thanh toán thành công
                            await _unitOfWork.GetCollection<UserSubscription>().InsertOneAsync(session, new UserSubscription
                            {
                                UserId = stripeSubscription.Metadata["user_id"],
                                SubscriptionId = stripeSubscription.Metadata["subscription_id"],
                                PeriodStart = HelperMethod.GetUtcPlus7TimeOffset(),
                                PeriodEnd = periodEnd,
                            });

                            UserRole userRole = stripeSubscription.Metadata["user_role"] switch
                            {
                                "Listener" => UserRole.Listener,
                                "Artist" => UserRole.Artist,
                                _ => throw new BadRequestCustomException("User role is not supported.")
                            };

                            // Cấp quyền entitlements
                            await _effectiveEntitlementService.RebuildTierAsync(session, stripeSubscription.Metadata["user_id"], userRole, stripeSubscription.Metadata["subscription_id"]);

                            break;
                        }

                    case EventTypes.CustomerSubscriptionUpdated: // Cập nhật currentSubscription của customer (ví dụ: gia hạn, thay đổi gói (upgrade/downgrade currentSubscription), hủy gói vào cuối kỳ)
                        {
                            StripeSubscription stripeSubscription = stripeEvent.Data.Object as StripeSubscription ?? throw new ArgumentNullCustomException("Subscription is NULL");

                            User user = await _unitOfWork.GetCollection<User>().Find(x => x.StripeCustomerId == stripeSubscription.CustomerId)
                                .Project<User>(Builders<User>.Projection
                                    .Include(x => x.Id)
                                    .Include(x => x.Email))
                                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user with the customer {stripeSubscription.CustomerId}");

                            string status = stripeSubscription.Status; // canceled, incomplete_expired, incomplete, trialing, active, past_due

                            // Case: Hủy vào cuối kỳ hạn
                            if (status == "active" && stripeSubscription.CancelAtPeriodEnd == true)
                            {
                                _logger.LogInformation($"Subscription {stripeSubscription.Id} will be canceled at the end of the period for user {user.Email}.");
                                // Không cần dùng background job để kiểm tra và hủy gói currentSubscription vào đúng ngày PeriodEnd
                                // Có thể gửi email nhắc nhở user vào khoảng n ngày trước PeriodEnd
                                // Vì vào đúng ngày PeriodEnd, Stripe sẽ gửi webhook CustomerSubscriptionDeleted với status = "canceled"
                                // Do đó, không cần làm gì hết
                            }

                            // TODO: Xử lý khi có thay đổi gói (upgrade/downgrade currentSubscription)

                            break;
                        }

                    case EventTypes.CustomerSubscriptionDeleted: // Hủy currentSubscription của customer
                        {
                            StripeSubscription stripeSubscription = stripeEvent.Data.Object as StripeSubscription ?? throw new ArgumentNullCustomException("Subscription is NULL");

                            string userId = await _unitOfWork.GetCollection<User>().Find(x => x.StripeCustomerId == stripeSubscription.CustomerId)
                                .Project(x => x.Id)
                                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user with the customer {stripeSubscription.CustomerId}");

                            string status = stripeSubscription.Status; // canceled, incomplete_expired, incomplete, trialing, active, past_due

                            // Case 1: Hủy ngay lập tức (có thể do thẻ hết hạn, không đủ tiền, v.v.)
                            // Khi user đã hủy đúng kỳ hạn thì event với status này sẽ được bắn ra vào đúng ngày PeriodEnd
                            // Và thêm điều kiện là CancelAtPeriodEnd = true
                            // Nhưng do đây là hủy currentSubscription nên không cần kiểm tra CancelAtPeriodEnd
                            if (status == "canceled")
                            {
                                // Cập nhật trạng thái UserSubscription thành Inactive/Deprecated
                                await _userSubscriptionService.UpdateStatusUserSubscriptionAsync(session, stripeSubscription.CancelAtPeriodEnd, HelperMethod.GetUtcPlus7TimeOffset(), false);

                                // Tạo mới UserSubscription mỗi lần có thanh toán thành công
                                await _userSubscriptionService.CreateUserSubscriptionAsync(session, string.Empty, HelperMethod.GetUtcPlus7TimeOffset());

                                // Hạ cấp quyền entitlements về Free
                                await _effectiveEntitlementService.RebuildFreeTierAsync(session, userId, UserRole.Listener);
                            }

                            // Case 2: Stripe không tự động charge được và phải thử lại nhiều lần
                            // Cái này hình như để ở customer.currentSubscription.updated
                            // Ngày 14/09 sẽ biết kết quả -> Kiểm tra trong dashboard webhook của Stripe
                            // Chưa biết được vì đang sandbox nên lúc nào cũng thanh toán thành công
                            else if (status == "past_due" || status == "unpaid")
                            {
                                _logger.LogInformation($"Subscription {stripeSubscription.Id} is past due or unpaid for user {userId}.");
                                // Không làm gì hết hoặc gửi email thông báo cho user
                            }

                            break;
                        }

                    default:
                        break;
                }
            }
            catch (StripeException e)
            {
                throw new ExternalServiceCustomException($"Stripe webhook error: {e}");
            }
        });
    }

    public async Task HandleWebhookInvoiceAsync(string json, string stripeSignature)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            try
            {
                Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTest);
                switch (stripeEvent.Type)
                {
                    // Case này thường xảy ra khi gia hạn currentSubscription và nó chỉ xảy ra khi thanh toán tự động thành công
                    // Nhưng lúc này event là InvoicePaid thay vì CustomerSubscriptionUpdated
                    // TODO: Thêm xử lý InvoicePaid
                    case EventTypes.InvoicePaid:
                        {
                            StripeInvoice invoice = stripeEvent.Data.Object as StripeInvoice ?? throw new ArgumentNullCustomException("NULL");

                            string customerId = invoice.CustomerId;
                            string stripeSubscriptionId = invoice.Parent.SubscriptionDetails.SubscriptionId;
                            string stripeProductId = invoice.Lines.Data[0].Pricing.PriceDetails.Product;

                            

                            switch (invoice.BillingReason)
                            {
                                case "subscription_create":
                                    {
                                        // Lần đầu tạo currentSubscription, đã được xử lý trong CustomerSubscriptionCreated
                                        break;
                                    }

                                case "subscription_cycle": // Gia hạn currentSubscription thành công
                                    {
                                        SubscriptionService subscriptionService = new();
                                        StripeSubscription stripeSubcription = await subscriptionService.GetAsync(stripeSubscriptionId);

                                        ProductService productService = new();
                                        Product product = await productService.GetAsync(stripeProductId);

                                        string currentSubscriptionId = product.Metadata["subscription_id"];
                                        SubscriptionTier currentSubscriptionTier = Enum.Parse<SubscriptionTier>(product.Metadata["subscription_tier"]);
                                        int currentSubscriptionVersion = Convert.ToInt32(product.Metadata["subscription_version"]);

                                        string userId = await _unitOfWork.GetCollection<User>().Find(x => x.StripeCustomerId == customerId)
                                            .Project(x => x.Id)
                                            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user with the customer {customerId}");

                                        string stripePriceId = invoice.Lines.Data[0].Pricing.PriceDetails.Price;

                                        // Cần lấy version currentSubscription mới khi có chính sách gói bị thay đổi
                                        // TODO: Làm sao để lấy được version mới nhất của currentSubscription
                                        //FilterDefinition<SubscriptionPlan> filter = Builders<SubscriptionPlan>.Filter.ElemMatch(x => x.SubscriptionPlanPrices, e => e.StripePriceId == stripePriceId) &
                                        //    Builders<SubscriptionPlan>.Filter.Eq(x => x.StripeProductActive, true);

                                        //ProjectionDefinition<SubscriptionPlan> projection = Builders<SubscriptionPlan>.Projection
                                        //    .Include(x => x.Id)
                                        //    .Include(x => x.SubscriptionId)
                                        //    .Include(x => x.SubscriptionPlanPrices)
                                        //    .ElemMatch(x => x.SubscriptionPlanPrices, e => e.StripePriceId == stripePriceId); // Trả về đúng 1 phần tử trong list trong SubscriptionPlanPrices

                                        //SubscriptionPlan subscriptionPlan = await _unitOfWork.GetCollection<SubscriptionPlan>()
                                        //    .Find(filter)
                                        //    .Project<SubscriptionPlan>(projection)
                                        //    .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found active currentSubscription");

                                        // Lấy interval từ Stripe Price
                                        PriceService priceService = new();
                                        Price price = await priceService.GetAsync(stripePriceId);
                                        string interval = price.Recurring.Interval; // "day", "week", "month", or "year"

                                        //string subscriptionId = subscriptionPlan.SubscriptionId;
                                        //Subscription currentSubscription = await _unitOfWork.GetCollection<Subscription>()
                                        //    .Find(x => x.Id == currentSubscriptionId && x.Status == SubscriptionStatus.Active)
                                        //    .Project<Subscription>(Builders<Subscription>.Projection
                                        //        .Include(x => x.Id)
                                        //        .Include(x => x.Tier)
                                        //        .Include(x => x.Version))
                                        //    .FirstOrDefaultAsync();

                                        var maxVersion = await _unitOfWork.GetCollection<Subscription>()
                                            .Aggregate()
                                            .Match(x => x.Tier == currentSubscriptionTier && x.Status == SubscriptionStatus.Active)
                                            .Group(x => true, g => new { MaxVersion = g.Max(x => x.Version) })
                                            .FirstOrDefaultAsync();
                                        int? latestVersion = maxVersion?.MaxVersion ?? currentSubscriptionVersion;

                                        if (latestVersion > currentSubscriptionVersion) // Giả sử có thay đổi gói currentSubscription
                                        {
                                            // Gói version mới muốn thay đổi sang
                                            string latestSubscriptionId = await _unitOfWork.GetCollection<Subscription>()
                                             .Find(x => x.Tier == currentSubscriptionTier &&
                                                x.Status == SubscriptionStatus.Active &&
                                                x.Version == latestVersion)
                                             .Project(x => x.Id)
                                             .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found currentSubscription");

                                            FilterDefinition<SubscriptionPlan> subcriptionPlanFilter = Builders<SubscriptionPlan>.Filter.And(
                                                Builders<SubscriptionPlan>.Filter.Eq(x => x.SubscriptionId, latestSubscriptionId),
                                                Builders<SubscriptionPlan>.Filter.Eq(x => x.StripeProductActive, true),
                                                Builders<SubscriptionPlan>.Filter.ElemMatch(x => x.SubscriptionPlanPrices, e => e.Interval == Enum.Parse<PeriodTime>(interval)));

                                            // Tìm stripePriceId mới tương ứng với gói currentSubscription mới
                                            string lastestStripePriceId = await _unitOfWork.GetCollection<SubscriptionPlan>()
                                                .Find(subcriptionPlanFilter)
                                                .Project(x => x.SubscriptionPlanPrices.First().StripePriceId)
                                                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found stripe price id with the same {interval} or currentSubscription {latestSubscriptionId}");

                                            // Cập nhật currentSubscription của customer
                                            await subscriptionService.UpdateAsync(stripeSubcription.Id, new SubscriptionUpdateOptions
                                            {
                                                // Hiện tại thiết kế chỉ có duy nhất 1 item trong currentSubscription
                                                Items =
                                                [
                                                    new SubscriptionItemOptions // Thay đổi price của currentSubscription item
                                                {
                                                    Id = stripeSubcription.Items.Data[0].Id, // ID của subscription_item hiện tại
                                                    Price = lastestStripePriceId,             // PriceId mới muốn thay thế
                                                },
                                            ],
                                                //ProrationBehavior = "create_prorations" // Tạo proration nếu có thay đổi gói giữa chừng
                                            });

                                            // Cập nhật trạng thái UserSubscription thành Inactive/Deprecated
                                            await _userSubscriptionService.UpdateStatusUserSubscriptionAsync(session, false, HelperMethod.GetUtcPlus7TimeOffset(), false);

                                            // Tạo mới UserSubscription mỗi lần có thanh toán thành công
                                            await _userSubscriptionService.CreateUserSubscriptionAsync(session, latestSubscriptionId, HelperMethod.GetUtcPlus7TimeOffset());

                                            // Cấp quyền entitlements về currentSubscription tương ứng
                                            await _effectiveEntitlementService.RebuildTierAsync(session, userId, UserRole.Listener, latestSubscriptionId);

                                            break;
                                        }

                                        // Cập nhật trạng thái UserSubscription thành Inactive/Deprecated
                                        await _userSubscriptionService.UpdateStatusUserSubscriptionAsync(session, false, HelperMethod.GetUtcPlus7TimeOffset(), false);

                                        // Tạo mới UserSubscription mỗi lần có thanh toán thành công
                                        await _userSubscriptionService.CreateUserSubscriptionAsync(session, currentSubscriptionId, HelperMethod.GetUtcPlus7TimeOffset());

                                        // Cấp quyền entitlements về currentSubscription tương ứng
                                        await _effectiveEntitlementService.RebuildTierAsync(session, userId, UserRole.Listener, currentSubscriptionId);

                                        break;
                                    }

                                case "subscription_update":
                                    {
                                        // Thay đổi gói currentSubscription (upgrade/downgrade)
                                        // Xử lý thay đổi UserSubscription
                                        break;
                                    }
                            }

                            break;
                        }
                    default:
                        break;
                }
            }
            catch (StripeException e)
            {
                throw new ExternalServiceCustomException($"Stripe webhook error: {e}");
            }
        });
    }

    // TODO: Xử lý webhook từ Stripe cho Express Connected Account (tạm thời chỉ log ra)
    public void HandleWebhookExpressConnectedAccount(string json, string stripeSignature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTest);

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

    public async Task HandleWebhookCheckoutSessionAsync(string json, string stripeSignature)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            try
            {
                Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTest);
                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    CheckoutOption.Session checkoutSession = stripeEvent.Data.Object as CheckoutOption.Session ?? throw new ArgumentNullCustomException("Checkout session is NULL");

                    // Cập nhật Transaction
                    UpdateDefinition<Transaction> update = Builders<Transaction>.Update
                        .Set(t => t.StripePaymentId, checkoutSession.PaymentIntentId)
                        .Set(t => t.StripePaymentMethod, checkoutSession.PaymentMethodTypes)
                        .Set(t => t.PaymentStatus, PaymentStatus.Paid)
                        .Set(t => t.Status, TransactionStatus.Completed)
                        .Set(t => t.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                    Transaction transaction = await _unitOfWork.GetCollection<Transaction>().FindOneAndUpdateAsync(session, Builders<Transaction>.Filter.Eq(x => x.StripeCheckoutSessionId, checkoutSession.Id), update);

                    // Tạo Invoice
                    await _unitOfWork.GetCollection<Domain.Entities.Invoice>().InsertOneAsync(session, new Domain.Entities.Invoice
                    {
                        UserId = transaction.UserId,
                        SubscriptionId = transaction.SubscriptionId,
                        SubscriptionPlanId = transaction.SubscriptionPlanId,
                        TransactionId = transaction.Id,

                        StripePaymentId = checkoutSession.PaymentIntentId,
                        StripePaymentMethod = checkoutSession.PaymentMethodTypes,

                        OriginContext = checkoutSession.OriginContext,

                        FullName = checkoutSession.Customer.Name,
                        Email = checkoutSession.Customer.Email,
                        Country = "VN", // Tạm thời
                        Amount = transaction.Amount,
                        Currency = transaction.Currency,

                        From = checkoutSession.Customer.Email,
                        To = "Ekofy" // Tạm thời
                    });
                }
            }
            catch (StripeException e)
            {
                throw new ExternalServiceCustomException($"Stripe webhook error: {e}");
            }
        });
    }
}
