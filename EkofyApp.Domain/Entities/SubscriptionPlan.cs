using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class SubscriptionPlan
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string SubscriptionId { get; set; } = null!;

    #region Stripe Product
    public string StripeProductId { get; set; } = null!;
    public bool StripeProductActive { get; set; }
    public string StripeProductName { get; set; } = null!;
    public List<string>? StripeProductImages { get; set; } = null;
    public string StripeProductType { get; set; } = null!; // "service" or "good"
    public List<Metadata>? StripeProductMetadata { get; set; } = null;
    #endregion

    #region Stripe Price
    public List<SubscriptionPlanPrice> SubscriptionPlanPrices { get; set; } = [];
    //public string StripePriceId { get; set; } = null!;
    //public bool StripePriceActive { get; set; }
    //public long StripePriceUnitAmount { get; set; }
    //public string StripePriceCurrency { get; set; } = null!;

    //public string StripePriceLookupKey { get; set; } = null!;
    //public List<Metadata>? StripePriceMetadata { get; set; } = null;

    //public PeriodTime Interval { get; set; }  // "day", "week", "month", or "year"
    //public int IntervalCount { get; set; } // e.g., every 3 months
    #endregion
}
