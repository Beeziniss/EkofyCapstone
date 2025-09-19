using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Transaction : TimeStamped, IEntityCustom // Snapshot of a payment transaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    #region Subscription
    public string SubscriptionName { get; set; } = null!;
    public string? SubscriptionDescription { get; set; }
    public string SubscriptionCode { get; set; } = null!; // Unique code for the subscription
    public int SubscriptionVersion { get; init; }

    public decimal SubscriptionAmount { get; set; }
    public CurrencyType SubscriptionCurrency { get; set; } = CurrencyType.vnd; // Default currency is vnd

    public SubscriptionTier SubscriptionTier { get; set; } // TODO: Cân nhắc có nên embed không
    public SubscriptionStatus SubscriptionStatus { get; set; }
    #endregion

    #region Subscription Plan
    public List<SubscriptionPlanPrice> SubscriptionPlanPrices { get; set; } = null!; // Snapshot of the prices at the time of transaction
    public string StripeProductId { get; set; } = null!;
    public bool StripeProductActive { get; set; }
    public string StripeProductName { get; set; } = null!;
    public List<string>? StripeProductImages { get; set; } = null;
    public string StripeProductType { get; set; } = null!; // "service" or "good"
    public List<Metadata>? StripeProductMetadata { get; set; } = null;
    #endregion

    #region Stripe
    public string StripeCheckoutSessionId { get; set; } = null!; // ID of the Checkout Session in Stripe
    public string? StripePaymentId { get; set; }// ID of the payment in Stripe
    public List<string> StripePaymentMethod { get; set; } = null!; // e.g., "visa", "master_card", "link"
    #endregion

    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!; // e.g., "usd", "vnd"

    public PaymentStatus PaymentStatus { get; set; } // e.g., "pending", "paid", "unpaid"
    public TransactionStatus Status { get; set; } // e.g., "open", "completed", "expired"
}

