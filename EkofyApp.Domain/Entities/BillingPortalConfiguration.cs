using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.BillingPortalConfig;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class BillingPortalConfiguration : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string StripeBillingPortalConfigurationId { get; set; } = null!;

    public UserRole UserRole { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; }
    public int Version { get; set; }

    // Customer
    public bool CustomerUpdateEnabled { get; set; }
    public List<CustomerUpdate> AllowedCustomerUpdates { get; set; } = [];

    // Payment Method
    public bool PaymentMethodUpdateEnabled { get; set; }

    // Invoice
    public bool InvoiceHistoryEnabled { get; set; }

    // Subscription Cancel
    public bool SubscriptionCancelEnabled { get; set; }
    public StripeSubscriptionCancelMode Mode { get; set; }

    // Subscription Update
    public bool SuscriptionUpdateEnabled { get; set; }
    public List<StripeSubscriptionUpdate> AllowedSubscriptionUpdates { get; set; } = [];
    public List<StripeProduct> Products { get; set; } = [];

    public DateTimeOffset? DeletedAt { get; set; }
}
