using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Settings;
using EkofyApp.Domain.Utils;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using StackExchange.Redis;
using Stripe;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Payment.Stripes;
public sealed class StripeWebhookService(IUnitOfWork unitOfWork, ILogger<StripeService> logger, IUserSubscriptionService userSubscriptionService, IEffectiveEntitlementService effectiveEntitlementService, StripeSetting stripeSetting, IRedisCacheService redisCacheService) : IStripeWebhookService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<StripeService> _logger = logger;
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;
    private readonly IEffectiveEntitlementService _effectiveEntitlementService = effectiveEntitlementService;
    private readonly StripeSetting _stripeSetting = stripeSetting;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    #region Idempotency Methods

    /// <summary>
    /// Kiểm tra xem event có nên được xử lý không.
    /// - Nếu đã processed → return true (skip)
    /// - Nếu chưa processed nhưng đã retry >= 3 lần → mark as processed và return true (skip)
    /// - Nếu chưa processed và retry < 3 lần → return false (continue processing)
    /// </summary>
    private async Task<bool> ShouldSkipEventAsync(string eventId)
    {
        try
        {
            // Kiểm tra xem đã processed chưa
            string processedKey = $"stripe_webhook_processed:{eventId}";
            if (await _redisCacheService.ExistsAsync(processedKey))
            {
                return true; // Đã processed, skip
            }

            // Kiểm tra retry count
            string retryKey = $"stripe_webhook_retry:{eventId}";
            const int maxRetries = 3;
            
            // Increment retry counter với TTL 24 giờ
            long retryCount = await _redisCacheService.HashIncrementAsync(retryKey, "count", 1);
            
            // Set TTL cho retry counter (24 hours) cho lần đầu
            if (retryCount == 1)
            {
                await _redisCacheService.SetExpirationAsync(retryKey, TimeSpan.FromHours(24));
            }
            
            if (retryCount >= maxRetries)
            {
                _logger.LogWarning($"Event {eventId} has failed {retryCount} times. Marking as processed to prevent further retries.");
                
                // Tự động đánh dấu đã xử lý để ngăn retry tiếp
                await MarkEventAsProcessedAsync(eventId, "failed_max_retries", "auto_marked");
                
                // Dọn dẹp retry counter
                await _redisCacheService.RemoveAsync(retryKey);
                
                return true; // Bỏ qua xử lý
            }
            
            _logger.LogInformation($"Event {eventId} retry attempt {retryCount}/{maxRetries}");
            return false; // Tiếp tục xử lý
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to check event status for {eventId}: {ex.Message}. Continuing with processing.");
            return false; // Tiếp tục xử lý nếu Redis fail
        }
    }

    /// <summary>
    /// Đánh dấu event đã được xử lý thành công
    /// TTL: 30 ngày (theo khuyến nghị Stripe)
    /// </summary>
    private async Task MarkEventAsProcessedAsync(string eventId, string eventType, string? webhookEndpoint = null)
    {
        try
        {
            string redisKey = $"stripe_webhook_processed:{eventId}";
            string redisValue = $"{eventType}|{webhookEndpoint}|{HelperMethod.GetUtcPlus7TimeOffset():yyyy-MM-dd HH:mm:ss}";

            // TTL 30 ngày theo khuyến nghị Stripe
            TimeSpan ttl = TimeSpan.FromDays(30);

            await _redisCacheService.SetStringAsync(redisKey, redisValue, ttl);
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError($"Failed to mark event {eventId} as processed in Redis: {stripeEx.Message}");
        }
        catch (RedisException redisEx)
        {
            _logger.LogError($"Redis error while marking event {eventId} as processed: {redisEx.Message}");
        }
    }

    #endregion

    // TODO: Xử lý webhook từ Stripe cho Customer (tạm thời chỉ log ra)
    // Resolved: Hoàn thành xử lý webhook Customer
    public async Task HandleWebhookCustomerAsync(string json, string stripeSignature)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            try
            {
                Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSetting.CustomerSigningSecret);

                // Kiểm tra xem event có nên skip không (đã processed hoặc retry quá 3 lần)
                if (await ShouldSkipEventAsync(stripeEvent.Id))
                {
                    return; // Bỏ qua xử lý
                }

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

                            // Hủy vào cuối kỳ hạn
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

                            // Hủy ngay lập tức (có thể do thẻ hết hạn, không đủ tiền, v.v.)
                            // Khi user đã hủy đúng kỳ hạn thì event với status này sẽ được bắn ra vào đúng ngày PeriodEnd
                            // Và thêm điều kiện là CancelAtPeriodEnd = true
                            // Nhưng do đây là hủy currentSubscription nên không cần kiểm tra CancelAtPeriodEnd
                            if (status == "canceled")
                            {
                                // Cập nhật trạng thái UserSubscription thành Inactive/Deprecated
                                await _userSubscriptionService.UpdateStatusUserSubscriptionAsync(session, userId, stripeSubscription.CancelAtPeriodEnd, HelperMethod.GetUtcPlus7TimeOffset(), false);

                                // Tạo mới UserSubscription mỗi lần có thanh toán thành công
                                await _userSubscriptionService.CreateUserSubscriptionAsync(session, userId, string.Empty, HelperMethod.GetUtcPlus7TimeOffset());

                                // Hạ cấp quyền entitlements về Free
                                await _effectiveEntitlementService.RebuildFreeTierAsync(session, userId, UserRole.Listener);
                            }

                            // Stripe không tự động charge được và phải thử lại nhiều lần
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

                // Đánh dấu event đã được xử lý thành công
                await MarkEventAsProcessedAsync(stripeEvent.Id, stripeEvent.Type, "customers");
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
                Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSetting.InvoiceSigningSecret);
                
                // Kiểm tra xem event có nên skip không (đã processed hoặc retry quá 3 lần)
                if (await ShouldSkipEventAsync(stripeEvent.Id))
                {
                    return; // Bỏ qua xử lý
                }

                switch (stripeEvent.Type)
                {
                    // Thường xảy ra khi gia hạn currentSubscription và nó chỉ xảy ra khi thanh toán tự động thành công
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
                                        //    .Include(x => x.UserId)
                                        //    .Include(x => x.SubscriptionId)
                                        //    .Include(x => x.SubscriptionPlanPrices)
                                        //    .ElemMatch(x => x.SubscriptionPlanPrices, e => e.StripePriceId == stripePriceId); // Trả về đúng 1 phần tử trong list trong SubscriptionPlanPrices

                                        //SubscriptionPlan subscriptionPlan = await _unitOfWork.GetCollection<SubscriptionPlan>()
                                        //    .Find(filter)
                                        //    .Project<SubscriptionPlan>(projection)
                                        //    .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found active currentSubscription");

                                        // Lấy interval từ Stripe Amount
                                        PriceService priceService = new();
                                        Price price = await priceService.GetAsync(stripePriceId);
                                        string interval = price.Recurring.Interval; // "day", "week", "month", or "year"

                                        //string subscriptionId = subscriptionPlan.SubscriptionId;
                                        //Subscription currentSubscription = await _unitOfWork.GetCollection<Subscription>()
                                        //    .Find(x => x.UserId == currentSubscriptionId && x.Status == SubscriptionStatus.Active)
                                        //    .Project<Subscription>(Builders<Subscription>.Projection
                                        //        .Include(x => x.UserId)
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
                                            await _userSubscriptionService.UpdateStatusUserSubscriptionAsync(session, userId, false, HelperMethod.GetUtcPlus7TimeOffset(), false);

                                            // Tạo mới UserSubscription mỗi lần có thanh toán thành công
                                            await _userSubscriptionService.CreateUserSubscriptionAsync(session, userId, latestSubscriptionId, HelperMethod.GetUtcPlus7TimeOffset());

                                            // Cấp quyền entitlements về currentSubscription tương ứng
                                            await _effectiveEntitlementService.RebuildTierAsync(session, userId, UserRole.Listener, latestSubscriptionId);

                                            break;
                                        }

                                        // Cập nhật trạng thái UserSubscription thành Inactive/Deprecated
                                        await _userSubscriptionService.UpdateStatusUserSubscriptionAsync(session, userId, false, HelperMethod.GetUtcPlus7TimeOffset(), false);

                                        // Tạo mới UserSubscription mỗi lần có thanh toán thành công
                                        await _userSubscriptionService.CreateUserSubscriptionAsync(session, userId, currentSubscriptionId, HelperMethod.GetUtcPlus7TimeOffset());

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

                // Đánh dấu event đã được xử lý thành công
                await MarkEventAsProcessedAsync(stripeEvent.Id, stripeEvent.Type, "invoice");
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
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSetting.AccountV2SigningSecret);

            // Kiểm tra xem event có nên skip không (đã processed hoặc retry quá 3 lần)
            // Note: Tạm thời sử dụng sync check vì method này không async
            bool shouldSkip = Task.Run(async () => await ShouldSkipEventAsync(stripeEvent.Id)).Result;
            if (shouldSkip)
            {
                return; // Bỏ qua xử lý
            }

            if (stripeEvent.Type == EventTypes.AccountUpdated)
            {
                Account account = stripeEvent.Data.Object as Account ?? throw new ArgumentNullCustomException("NULL");
                _logger.LogInformation($"Account updated: {account.Id}, Email: {account.Email}, ChargesEnabled: {account.ChargesEnabled}");
            }

            // Đánh dấu event đã được xử lý thành công
            // Note: Tạm thời sử dụng sync mark vì method này không async
            Task.Run(async () => await MarkEventAsProcessedAsync(stripeEvent.Id, stripeEvent.Type, "v1/accounts"));
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
                Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSetting.CheckoutSessionSigningSecret);
                
                // Kiểm tra xem event có nên skip không (đã processed hoặc retry quá 3 lần)
                if (await ShouldSkipEventAsync(stripeEvent.Id))
                {
                    return; // Bỏ qua xử lý
                }

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    CheckoutOption.Session checkoutSession = stripeEvent.Data.Object as CheckoutOption.Session ?? throw new ArgumentNullCustomException("Checkout session is NULL");

                    // Cập nhật PaymentTransaction
                    UpdateDefinition<PaymentTransaction> update = Builders<PaymentTransaction>.Update
                        .Set(t => t.StripePaymentId, checkoutSession.PaymentIntentId)
                        .Set(t => t.StripePaymentMethod, checkoutSession.PaymentMethodTypes)
                        .Set(t => t.PaymentStatus, PaymentTransactionStatus.Paid)
                        .Set(t => t.Status, TransactionStatus.Completed)
                        .Set(t => t.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                    PaymentTransaction transaction = await _unitOfWork.GetCollection<PaymentTransaction>().FindOneAndUpdateAsync(session, Builders<PaymentTransaction>.Filter.Eq(x => x.StripeCheckoutSessionId, checkoutSession.Id), update);

                    // Kiểm tra nếu đây là escrow payment
                    if (checkoutSession.Metadata.TryGetValue("is_escrow", out string? value) && Convert.ToBoolean(value))
                    {
                        await HandleEscrowPaymentAsync(session, checkoutSession, transaction);
                        return; // Escrow payment không cần xử lý subscription/one-off logic
                    }

                    OneOffSnapshot? oneOffSnapshot = null;
                    SubscriptionSnapshot? subscriptionSnapshot = null;
                    if (Convert.ToBoolean(checkoutSession.Metadata["is_subscription"]))
                    {
                        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
                            .Find(x => x.Code == checkoutSession.Metadata["subscription_code"] &&
                                x.Status == SubscriptionStatus.Active)
                            //.Project<Subscription>(Builders<Subscription>.Projection
                            //    .Include(x => x.UserId))
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
                                .ElemMatch(x => x.SubscriptionPlanPrices, p => p.Interval == Enum.Parse<PeriodTime>(checkoutSession.Metadata["subscription_period"])))
                            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription plan's price.");

                        subscriptionSnapshot = new()
                        {
                            SubscriptionName = subscription.Name,
                            SubscriptionDescription = subscription.Description,
                            SubscriptionCode = subscription.Code,
                            SubscriptionVersion = subscription.Version,
                            SubscriptionAmount = subscription.Amount,
                            SubscriptionCurrency = subscription.Currency,
                            SubscriptionTier = subscription.Tier,
                            SubscriptionStatus = subscription.Status,

                            // Gói đăng ký
                            SubscriptionPlanPrices = subscriptionPlan.SubscriptionPlanPrices,
                            StripeProductId = subscriptionPlan.StripeProductId,
                            StripeProductActive = subscriptionPlan.StripeProductActive,
                            StripeProductName = subscriptionPlan.StripeProductName,
                            StripeProductImages = subscriptionPlan.StripeProductImages,
                            StripeProductType = subscriptionPlan.StripeProductType,
                            StripeProductMetadata = subscriptionPlan.StripeProductMetadata,
                        };
                    }
                    else
                    {
                        oneOffSnapshot = new()
                        {
                            PackageName = checkoutSession.Metadata["package_name"],
                            PackageAmount = Convert.ToDecimal(checkoutSession.Metadata["package_amount"]),
                            PackageCurrency = Enum.Parse<CurrencyType>(checkoutSession.Metadata["package_currency"]),
                            Description = checkoutSession.Metadata["package_description"],
                            //ServiceDetails = artistPackage.ServiceDetails,
                            Status = Enum.Parse<ArtistPackageStatus>(checkoutSession.Metadata["package_status"]),
                        };
                    }

                    // Tạo Invoice
                    await _unitOfWork.GetCollection<Domain.Entities.Invoice>().InsertOneAsync(session, new Domain.Entities.Invoice
                    {
                        UserId = transaction.UserId,
                        PaymentTransactionId = transaction.Id,

                        OneOffSnapshot = oneOffSnapshot,
                        SubscriptionSnapshot = subscriptionSnapshot,

                        //OriginContext = checkoutSession.OriginContext,

                        FullName = checkoutSession.CustomerDetails.Name,
                        Email = checkoutSession.CustomerDetails.Email,
                        Country = "VN", // Tạm thời
                        Amount = transaction.Amount,
                        Currency = transaction.Currency,

                        From = checkoutSession.CustomerDetails.Email,
                        To = "Ekofy" // Tạm thời
                    });
                }

                // Đánh dấu event đã được xử lý thành công
                await MarkEventAsProcessedAsync(stripeEvent.Id, stripeEvent.Type, "checkout-session");
            }
            catch (StripeException e)
            {
                throw new ExternalServiceCustomException($"Stripe webhook error: {e}");
            }
        });
    }

    // Xử lý payout webhook events từ Stripe
    public async Task HandleWebhookPayoutAsync(string json, string stripeSignature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSetting.PayoutSigningSecret);
            
            // Kiểm tra xem event có nên skip không (đã processed hoặc retry quá 3 lần)
            if (await ShouldSkipEventAsync(stripeEvent.Id))
            {
                return; // Bỏ qua xử lý
            }

            Payout payout = stripeEvent.Data.Object as Payout ?? throw new ArgumentNullCustomException("Payout is NULL");

            // Xử lý các sự kiện payout khác nhau
            switch (stripeEvent.Type)
            {
                case EventTypes.PayoutPaid:
                    {
                        // Cập nhật payout transactions từ pending hoặc in_transit status thành paid
                        UpdateDefinition<PayoutTransaction> updateDefinition = Builders<PayoutTransaction>.Update
                            .Set(x => x.Status, Enum.Parse<PayoutTransactionStatus>(payout.Status))
                            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                        UpdateResult updateResult = await _unitOfWork.GetCollection<PayoutTransaction>()
                            .UpdateManyAsync(
                                x => x.StripePayoutId == payout.Id &&
                                 (x.Status == PayoutTransactionStatus.pending || x.Status == PayoutTransactionStatus.in_transit),
                                updateDefinition);
                        if (updateResult.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException($"No payout transactions were updated for Stripe Payout ID: {payout.Id}");
                        }

                        break;
                    }

                case EventTypes.PayoutFailed:
                    {
                        // Cập nhật payout transactions từ pending hoặc in_transit status thành failed
                        UpdateDefinition<PayoutTransaction> updateDefinition = Builders<PayoutTransaction>.Update
                            .Set(x => x.Status, Enum.Parse<PayoutTransactionStatus>(payout.Status))
                            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                        UpdateResult updateResult = await _unitOfWork.GetCollection<PayoutTransaction>()
                            .UpdateManyAsync(
                                x => x.StripePayoutId == payout.Id &&
                                 (x.Status == PayoutTransactionStatus.pending || x.Status == PayoutTransactionStatus.in_transit),
                                updateDefinition);
                        if (updateResult.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException($"No payout transactions were updated for Stripe Payout ID: {payout.Id}");
                        }

                        break;
                    }

                case EventTypes.PayoutCanceled:
                    {
                        // Cập nhật payout transactions từ pending status thành canceled
                        // Note: Only pending payouts can be canceled, not in_transit ones
                        UpdateDefinition<PayoutTransaction> updateDefinition = Builders<PayoutTransaction>.Update
                            .Set(x => x.Status, Enum.Parse<PayoutTransactionStatus>(payout.Status))
                            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                        UpdateResult updateResult = await _unitOfWork.GetCollection<PayoutTransaction>()
                            .UpdateManyAsync(
                                x => x.StripePayoutId == payout.Id && x.Status == PayoutTransactionStatus.pending,
                                updateDefinition);
                        if (updateResult.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException($"No payout transactions were updated for Stripe Payout ID: {payout.Id}");
                        }

                        break;
                    }

                case EventTypes.PayoutUpdated:
                    {
                        // Xử lý chuyển trạng thái: pending → in_transit
                        // Thường xảy ra khi payout chuyển từ pending thành in_transit
                        UpdateDefinition<PayoutTransaction> updateDefinition = Builders<PayoutTransaction>.Update
                            .Set(x => x.Status, Enum.Parse<PayoutTransactionStatus>(payout.Status))
                            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                        UpdateResult updateResult = await _unitOfWork.GetCollection<PayoutTransaction>()
                            .UpdateManyAsync(
                              x => x.StripePayoutId == payout.Id && x.Status != Enum.Parse<PayoutTransactionStatus>(payout.Status),
                              updateDefinition);
                        if (updateResult.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException($"No payout transactions were updated for Stripe Payout ID: {payout.Id}");
                        }

                        break;
                    }
            }

            // Đánh dấu event đã được xử lý thành công
            await MarkEventAsProcessedAsync(stripeEvent.Id, stripeEvent.Type, "payout");
        }
        catch (StripeException e)
        {
            throw new ExternalServiceCustomException($"Stripe payout webhook error: {e}");
        }
        catch (Exception ex)
        {
            throw new ExternalServiceCustomException($"Error processing payout webhook: {ex.Message}");
        }
    }

    // Xử lý refund webhook events từ Stripe
    public async Task HandleWebhookRefundAsync(string json, string stripeSignature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSetting.RefundWebhookSecret);

            // Kiểm tra xem event có nên được xử lý không
            if (await ShouldSkipEventAsync(stripeEvent.Id))
            {
                _logger.LogInformation("Skipping already processed refund webhook event: {EventId}", stripeEvent.Id);
                return;
            }

            Refund refund = stripeEvent.Data.Object as Refund ?? throw new ArgumentNullCustomException("Refund object is NULL");

            switch (stripeEvent.Type)
            {
                case EventTypes.ChargeRefunded:
                    {
                        // Cập nhật trạng thái refund thành succeeded
                        UpdateDefinition<RefundTransaction> updateDefinition = Builders<RefundTransaction>.Update
                            .Set(x => x.Status, RefundTransactionStatus.Succeeded)
                            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                        UpdateResult updateResult = await _unitOfWork.GetCollection<RefundTransaction>()
                            .UpdateOneAsync(
                                x => x.StripeRefundId == refund.Id,
                                updateDefinition);

                        if (updateResult.ModifiedCount == 0)
                        {
                            _logger.LogWarning("No refund transaction found for Stripe Refund ID: {RefundId}", refund.Id);
                        }

                        _logger.LogInformation("Refund succeeded: {RefundId}", refund.Id);
                        break;
                    }

                case EventTypes.ChargeRefundUpdated:
                    {
                        // Cập nhật trạng thái refund
                        var refundStatus = Enum.Parse<RefundTransactionStatus>(refund.Status, true);
                        
                        UpdateDefinition<RefundTransaction> updateDefinition = Builders<RefundTransaction>.Update
                            .Set(x => x.Status, refundStatus)
                            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                        UpdateResult updateResult = await _unitOfWork.GetCollection<RefundTransaction>()
                            .UpdateOneAsync(
                                x => x.StripeRefundId == refund.Id,
                                updateDefinition);

                        if (updateResult.ModifiedCount == 0)
                        {
                            _logger.LogWarning("No refund transaction found for Stripe Refund ID: {RefundId}", refund.Id);
                        }

                        _logger.LogInformation("Refund updated: {RefundId}, Status: {Status}", refund.Id, refund.Status);
                        break;
                    }

                case EventTypes.ChargeDisputeCreated:
                    {
                        // Xử lý dispute (chargeback) - tạo refund record cho dispute
                        Dispute dispute = stripeEvent.Data.Object as Dispute ?? throw new ArgumentNullCustomException("Dispute object is NULL");
                        
                        // Tìm payment transaction từ charge ID
                        PaymentTransaction? paymentTransaction = await _unitOfWork.GetCollection<PaymentTransaction>()
                            .Find(x => x.StripePaymentId != null && x.StripePaymentId.Contains(dispute.ChargeId))
                            .FirstOrDefaultAsync();

                        if (paymentTransaction != null)
                        {
                            // Tạo refund transaction cho dispute
                            var disputeRefund = new RefundTransaction
                            {
                                UserId = paymentTransaction.UserId,
                                PaymentTransactionId = paymentTransaction.Id,
                                StripeRefundId = dispute.Id, // Sử dụng dispute ID
                                StripePaymentIntentId = paymentTransaction.StripePaymentId!,
                                StripeChargeId = dispute.ChargeId,
                                Amount = Convert.ToDecimal(dispute.Amount) / 100, // Convert from cents
                                Currency = dispute.Currency,
                                Type = RefundType.Full, // Dispute thường là full refund
                                Status = RefundTransactionStatus.Pending, // Dispute đang pending
                                Reason = dispute.Reason,
                                Description = $"Dispute: {dispute.Reason}",
                                Metadata = new Dictionary<string, string>
                                {
                                    { "dispute_id", dispute.Id },
                                    { "dispute_reason", dispute.Reason },
                                    { "is_dispute", "true" }
                                }
                            };

                            await _unitOfWork.GetCollection<RefundTransaction>().InsertOneAsync(disputeRefund);
                            _logger.LogInformation("Dispute created and refund record added: {DisputeId} for Charge: {ChargeId}", dispute.Id, dispute.ChargeId);
                        }
                        else
                        {
                            _logger.LogWarning("Payment transaction not found for dispute charge: {ChargeId}", dispute.ChargeId);
                        }

                        break;
                    }
            }

            // Đánh dấu event đã được xử lý thành công
            await MarkEventAsProcessedAsync(stripeEvent.Id, stripeEvent.Type, "refund");
        }
        catch (StripeException e)
        {
            throw new ExternalServiceCustomException($"Stripe refund webhook error: {e}");
        }
        catch (Exception ex)
        {
            throw new ExternalServiceCustomException($"Error processing refund webhook: {ex.Message}");
        }
    }

    /// <summary>
    /// Xử lý escrow payment khi checkout session completed
    /// </summary>
    private async Task HandleEscrowPaymentAsync(IClientSessionHandle session, CheckoutOption.Session checkoutSession, PaymentTransaction transaction)
    {
        try
        {
            // Parse metadata từ checkout session
            var metadata = checkoutSession.Metadata;
            var artistPackageId = metadata["artist_package_id"];
            var artistId = metadata["artist_id"];
            var buyerId = metadata["buyer_id"];
            var artistStripeAccount = metadata["artist_stripe_account"];
            
            var advancePercentage = decimal.Parse(metadata["advance_percentage"]);
            var completionPercentage = decimal.Parse(metadata["completion_percentage"]);
            var platformCommissionPercentage = decimal.Parse(metadata["platform_commission_percentage"]);
            
            var advanceAmount = decimal.Parse(metadata["advance_amount"]);
            var completionAmount = decimal.Parse(metadata["completion_amount"]);
            var platformCommission = decimal.Parse(metadata["platform_commission"]);
            
            var estimatedDeliveryDays = int.Parse(metadata["estimated_delivery_days"]);
            var maxRevisions = int.Parse(metadata["max_revisions"]);

            // Tạo PaymentSplit embedded document
            var paymentSplit = new PaymentSplit
            {
                StripePaymentIntentId = checkoutSession.PaymentIntentId!,
                ArtistStripeAccountId = artistStripeAccount,
                
                TotalAmount = transaction.Amount,
                AdvancePaymentAmount = advanceAmount,
                CompletionPaymentAmount = completionAmount,
                PlatformCommissionAmount = platformCommission,
                
                AdvancePaymentPercentage = advancePercentage,
                CompletionPaymentPercentage = completionPercentage,
                PlatformCommissionPercentage = platformCommissionPercentage,
                
                Currency = transaction.Currency,
                Status = EscrowTransactionStatus.Pending,
                
                AutoReleaseDate = HelperMethod.GetUtcPlus7TimeOffset().AddDays(estimatedDeliveryDays + 7), // Auto release 7 days after estimated delivery
                CreatedAt = HelperMethod.GetUtcPlus7TimeOffset()
            };

            // Tạo ArtistPackageOrder với embedded PaymentSplit
            var order = new ArtistPackageOrder
            {
                ClientId = buyerId,
                ProviderId = artistId,
                ArtistPackageId = artistPackageId,
                PaymentTransactionId = transaction.Id,
                
                Status = ArtistPackageOrderStatus.Pending,
                OrderDescription = "Order created from escrow payment", // Will be updated by buyer
                EstimatedDeliveryDate = HelperMethod.GetUtcPlus7TimeOffset().AddDays(estimatedDeliveryDays),
                MaxRevisions = maxRevisions,
                
                EscrowPayment = paymentSplit // Embed PaymentSplit
            };

            await _unitOfWork.GetCollection<ArtistPackageOrder>().InsertOneAsync(session, order);

            // Tự động release advance payment (30%)
            await ReleaseAdvancePaymentForEscrowAsync(order.Id);

            _logger.LogInformation("Escrow payment processed successfully. Order ID: {OrderId}, Amount: {Amount}", 
                order.Id, transaction.Amount);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing escrow payment for checkout session: {CheckoutSessionId}", checkoutSession.Id);
            throw;
        }
    }

    /// <summary>
    /// Release advance payment cho escrow transaction (called from webhook)
    /// </summary>
    private async Task ReleaseAdvancePaymentForEscrowAsync(string orderId)
    {
        try
        {
            ArtistPackageOrder order = await _unitOfWork.GetCollection<ArtistPackageOrder>()
                .Find(x => x.Id == orderId && x.IsEscrowPayment)
                .FirstOrDefaultAsync();

            if (order?.EscrowPayment == null) return;

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

            _logger.LogInformation("Advance payment auto-released: {TransferId} for Order: {OrderId}, Amount: {Amount} {Currency}", 
                transfer.Id, orderId, order.EscrowPayment.AdvancePaymentAmount, order.EscrowPayment.Currency);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing advance payment for order: {OrderId}", orderId);
            throw;
        }
    }
}
