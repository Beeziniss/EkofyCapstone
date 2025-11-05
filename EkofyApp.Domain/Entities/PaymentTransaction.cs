using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class PaymentTransaction : TimeStamped, IEntityCustom // Snapshot of a payment transaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    #region Stripe
    public string StripeCheckoutSessionId { get; set; } = null!; // ID of the Checkout Session in Stripe
    public string? StripeSubscriptionId { get; set; }
    public string? StripeInvoiceId { get; set; } // ID of the Invoice in Stripe
    public string? StripePaymentId { get; set; } // ID of the payment in Stripe
    public List<string> StripePaymentMethod { get; set; } = null!; // e.g., "visa", "master_card", "link"
    #endregion

    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!; // e.g., "usd", "vnd"

    public PaymentTransactionStatus PaymentStatus { get; set; } // e.g., "pending", "paid", "unpaid"
    public TransactionStatus Status { get; set; } // e.g., "open", "completed", "expired"
}

