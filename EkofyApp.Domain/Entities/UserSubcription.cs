using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class UserSubcription : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the subscription

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user
    [BsonRepresentation(BsonType.ObjectId)]
    public string SubscriptionId { get; set; } = null!; // Unique identifier for the subscription plan

    public UserRole Role { get; set; }
    public SubscriptionTier Tier { get; set; }

    public Status Status { get; set; } = Status.Active; // Default status is Active

    public DateTime PeriodStart { get; set; } // Start date of the subscription period
    public DateTime PeriodEnd { get; set; } // End date of the subscription period

    public bool AutoRenew { get; set; } = false; // Indicates if the subscription auto-renews
    public bool CancelAtEndOfPeriod { get; set; } = false;
    public DateTime? CanceledAt { get; set; }

    // Provider linkage
    //public string? PaymentProvider { get; set; } // "Stripe", "Momo", ...
    //public string? ProviderCustomerId { get; set; }
    //public string? ProviderSubscriptionId { get; set; }
    //public string? LatestInvoiceId { get; set; }
}
