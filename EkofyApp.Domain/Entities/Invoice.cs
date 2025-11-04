using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Invoice : IEntityCustom // Snapshot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string PaymentTransactionId { get; set; } = null!;
    public string StripeInvoiceId { get; set; } = null!; // ID of the Invoice in Stripe

    public OneOffSnapshot? OneOffSnapshot { get; set; } // Snapshot of the one-off purchase
    public SubscriptionSnapshot? SubscriptionSnapshot { get; set; } // Snapshot of the subscription

    #region Stripe
    //public string StripeInvoiceId { get; set; } = null!; // ID of the invoice in Stripe
    //public string StripeInvoiceNumber { get; set; } = null!; // e.g., "0001"
    //public string StripeInvoicePdf { get; set; } = null!; // URL to the PDF of the invoice
    //public string StripeInvoiceDescription { get; set; } = null!; // Description of the invoice
    //public string StripeInvoiceStatementDescriptor { get; set; } = null!; // Statement descriptor for the invoice

    //public string StripePaymentId { get; set; } = null!; // ID of the payment in Stripe
    //public List<string> StripePaymentMethod { get; set; } = null!; // e.g., "visa", "master_card", "link"
    #endregion

    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Country { get; set; } = null!; // e.g., "US", "VN"
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!; // e.g., "usd", "vnd"

    public string From { get; set; } = null!; // e.g., "Ekofy Inc."
    public string To { get; set; } = null!; // e.g., "John Doe"

    public string? OriginContext { get; set; } // e.g., "web", "mobile"

    public DateTimeOffset PaidAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset();
}
