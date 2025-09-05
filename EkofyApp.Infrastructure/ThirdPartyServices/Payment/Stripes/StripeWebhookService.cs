using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Stripe;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Payment.Stripes;
public sealed class StripeWebhookService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ILogger<StripeService> logger) : IStripeWebhookService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<StripeService> _logger = logger;
    private readonly string WebhookSecretTest = Environment.GetEnvironmentVariable("STRIPE_SIGNATURE_SECRET_TEST") ?? throw new InvalidOperationException("STRIPE_SIGNATURE_SECRET_TEST is not configured.");

    // TODO: Xử lý webhook từ Stripe cho Customer (tạm thời chỉ log ra)
    public void HandleWebhookCustomer(string json, string stripeSignature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTest);

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

    public async Task HandleWebhookSubscriptionPlanAsync(string json, string stripeSignature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTest);
            if (stripeEvent.Type == EventTypes.PriceCreated)
            {
                Price price = stripeEvent.Data.Object as Price ?? throw new ArgumentNullCustomException("NULL");

                await _unitOfWork.ExecuteInTransactionAsync(async session =>
                {
                    await _unitOfWork.GetCollection<SubscriptionPlan>().InsertOneAsync(new SubscriptionPlan
                    {
                        SubscriptionId = price.Metadata["subscription_id"],
                        Interval = price.Recurring.Interval,
                        IntervalCount = Convert.ToInt16(price.Recurring.IntervalCount),

                        StripeProductId = price.ProductId,
                        StripeProductActive = price.Product.Active,
                        StripeProductName = price.Product.Name,
                        StripeProductImages = price.Product.Images,
                        StripeProductType = price.Product.Type,
                        StripeProductMetadata = price.Product.Metadata.Select(x => new Metadata { Key = x.Key, Value = x.Value }).ToList(),

                        StripePriceId = price.Id,
                        StripePriceActive = price.Active,
                        StripePriceUnitAmount = price.UnitAmount ?? 0,
                        StripePriceCurrency = price.Currency,
                        StripePriceLookupKey = price.LookupKey,
                        StripePriceMetadata = price.Metadata.Select(x => new Metadata { Key = x.Key, Value = x.Value }).ToList(),
                    });
                });
            }
        }
        catch (StripeException e)
        {
            _logger.LogError($"Webhook error: {e.Message}");
        }
    }

    public async Task HandleWebhookCheckoutSessionAsync(string json, string stripeSignature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, WebhookSecretTest);
            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                CheckoutOption.Session checkoutSession = stripeEvent.Data.Object as CheckoutOption.Session ?? throw new ArgumentNullCustomException("NULL");

                await _unitOfWork.ExecuteInTransactionAsync(async session =>
                {
                    UpdateDefinition<Transaction> update = Builders<Transaction>.Update
                    .Set(t => t.StripePaymentId, checkoutSession.PaymentIntentId)
                    .Set(t => t.StripePaymentMethod, checkoutSession.PaymentMethodTypes)
                    .Set(t => t.PaymentStatus, PaymentStatus.Paid)
                    .Set(t => t.Status, TransactionStatus.Completed)
                    .Set(t => t.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

                    Transaction transaction = await _unitOfWork.GetCollection<Transaction>().FindOneAndUpdateAsync(Builders<Transaction>.Filter.Eq(x => x.StripeCheckoutSessionId, checkoutSession.Id), update);

                    await _unitOfWork.GetCollection<Receipt>().InsertOneAsync(new Receipt
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
                        Country = "VN",
                        Amount = transaction.Amount,
                        Currency = transaction.Currency,
                        
                        From = checkoutSession.Customer.Email,
                        To = "Ekofy" // Tạm thời
                    });
                });
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
                    return $"Receipt {invoice.Id} paid.";

                case "customer.subscription.deleted":
                    var subDeleted = stripeEvent.Data.Object as StripeSubscription;
                    // Update DB: hủy premium cho listener
                    return $"Subscription {subDeleted.Id} cancelled.";

                case "checkout.checkoutSession.completed":
                    var session = stripeEvent.Data.Object as CheckoutOption.Session;
                    // Xử lý thanh toán 1 lần hoặc sub checkout
                    return $"Checkout completed for checkoutSession {session.Id}.";

                default:
                    return $"Unhandled event type: {stripeEvent.Type}";
            }
        }
        catch (StripeException e)
        {
            return $"Webhook error: {e.Message}";
        }
    }
}
