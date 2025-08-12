using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums.Subcriptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class UserSubscription : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the subscription

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user
    [BsonRepresentation(BsonType.ObjectId)]
    public string SubscriptionId { get; set; } = null!; // Unique identifier for the subscription plan

    public DateTime PeriodStart { get; set; } // Start date of the subscription period
    public DateTime PeriodEnd { get; set; } // End date of the subscription period

    // Nếu muốn gia hạn tự động thêm thì phải thực hiện ơ lần thanh toán tiếp theo (UserSubscription mới)
    public bool AutoRenew { get; set; } = false; // Indicates if the subscription auto-renews
    public bool CancelAtEndOfPeriod { get; set; } = false;
    public DateTime? CanceledAt { get; set; }

    // Optional: Additional fields for tracking subscription status
     public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active; // Default status is Active

    // Provider linkage
    //public string? PaymentProvider { get; set; } // "Stripe", "Momo", ...
    //public string? ProviderCustomerId { get; set; }
    //public string? ProviderSubscriptionId { get; set; }
    //public string? LatestInvoiceId { get; set; }
}
