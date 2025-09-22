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

    public OneOffSnapshot? OneOffSnapshot { get; set; } // Snapshot of the one-off purchase at the time of transaction
    public SubscriptionSnapshot? SubscriptionSnapshot { get; set; } // Snapshot of the subscription at the time of transaction

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

