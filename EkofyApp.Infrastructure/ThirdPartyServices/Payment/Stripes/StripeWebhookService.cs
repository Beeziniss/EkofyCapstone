using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using HotChocolate.Execution.Processing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Stripe;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Payment.Stripes;
public sealed class StripeWebhookService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ILogger<StripeService> logger, IUserSubscriptionService userSubscriptionService, IEffectiveEntitlementService effectiveEntitlementService) : IStripeWebhookService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
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

                    case EventTypes.CustomerSubscriptionCreated: // Đăng ký subscription mới của customer (bao gồm cả lần đầu)
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

                    case EventTypes.CustomerSubscriptionUpdated: // Cập nhật subscription của customer (ví dụ: gia hạn, thay đổi gói (upgrade/downgrade subscription), hủy gói vào cuối kỳ)
                        {
                            StripeSubscription stripeSubscription = stripeEvent.Data.Object as StripeSubscription ?? throw new ArgumentNullCustomException("Subscription is NULL");

                            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

                            string status = stripeSubscription.Status; // canceled, incomplete_expired, incomplete, trialing, active, past_due

                            // Case: Hủy vào cuối kỳ hạn
                            if (status == "active" && stripeSubscription.CancelAtPeriodEnd == true)
                            {
                                _logger.LogInformation($"Subscription {stripeSubscription.Id} will be canceled at the end of the period for user {userId}.");
                                // Không cần dùng background job để kiểm tra và hủy gói subscription vào đúng ngày PeriodEnd
                                // Có thể gửi email nhắc nhở user vào khoảng n ngày trước PeriodEnd
                                // Vì vào đúng ngày PeriodEnd, Stripe sẽ gửi webhook CustomerSubscriptionDeleted với status = "canceled"
                                // Do đó, không cần làm gì hết
                            }

                            // TODO: Xử lý khi có thay đổi gói (upgrade/downgrade subscription)

                            break;
                        }

                    case EventTypes.CustomerSubscriptionDeleted: // Hủy subscription của customer
                        {
                            StripeSubscription stripeSubscription = stripeEvent.Data.Object as StripeSubscription ?? throw new ArgumentNullCustomException("Subscription is NULL");

                            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

                            string status = stripeSubscription.Status; // canceled, incomplete_expired, incomplete, trialing, active, past_due

                            // Case 1: Hủy ngay lập tức (có thể do thẻ hết hạn, không đủ tiền, v.v.)
                            if (status == "canceled")
                            {
                                // Cập nhật trạng thái UserSubscription thành Inactive/Deprecated
                                await _unitOfWork.GetCollection<UserSubscription>().UpdateOneAsync(session, x => x.UserId == userId && x.IsActive == true,
                                    Builders<UserSubscription>.Update
                                    .Set(x => x.IsActive, false)
                                    .Set(x => x.CanceledAt, HelperMethod.GetUtcPlus7TimeOffset())
                                    .Set(x => x.CancelAtEndOfPeriod, false)
                                    .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                                );

                                await _userSubscriptionService.UpdateStatusUserSubscriptionAsync(session, false, HelperMethod.GetUtcPlus7TimeOffset(), false);

                                // Tạo mới UserSubscription mỗi lần có thanh toán thành công
                                await _userSubscriptionService.CreateUserSubscriptionAsync(session, string.Empty, HelperMethod.GetUtcPlus7TimeOffset());

                                // Hạ cấp quyền entitlements về Free
                                List<Entitlement> entitlements = await _unitOfWork.GetCollection<Subscription>()
                                    .Find(x => x.Tier == SubscriptionTier.Free && x.Status == SubscriptionStatus.Active)
                                    .Project(x => x.Entitlements)
                                    .FirstOrDefaultAsync();

                                await _effectiveEntitlementService.RebuildFreeTierAsync(session, userId, UserRole.Listener);
                            }

                            // Case 2: Stripe không tự động charge được và phải thử lại nhiều lần
                            // Cái này hình như để ở customer.subscription.updated
                            // Ngày 14/09 sẽ biết kết quả -> Kiểm tra trong dashboard webhook của Stripe
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
                    // Case này thường xảy ra khi gia hạn subscription và nó chỉ xảy ra khi thanh toán tự động thành công
                    // Nhưng lúc này event là InvoicePaid thay vì CustomerSubscriptionUpdated
                    // TODO: Thêm xử lý InvoicePaid
                    case EventTypes.InvoicePaid:
                        {
                            StripeInvoice invoice = stripeEvent.Data.Object as StripeInvoice ?? throw new ArgumentNullCustomException("NULL");
                            string customerId = invoice.CustomerId;

                            switch (invoice.BillingReason)
                            {
                                case "subscription_create":
                                    {
                                        // Lần đầu tạo subscription, đã được xử lý trong CustomerSubscriptionCreated
                                        break;
                                    }

                                case "subscription_cycle":
                                    {
                                        // Gia hạn subscription thành công
                                        // Xử lý gia hạn UserSubscription
                                        break;
                                    }

                                case "subscription_update":
                                    {
                                        // Thay đổi gói subscription (upgrade/downgrade)
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
