using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class SubscriptionPlanPrice
{
    public string StripePriceId { get; set; } = null!;
    public bool StripePriceActive { get; set; }
    public long StripePriceUnitAmount { get; set; }
    public string StripePriceCurrency { get; set; } = null!;

    public string StripePriceLookupKey { get; set; } = null!;
    public List<Metadata>? StripePriceMetadata { get; set; } = null;

    public PeriodTime Interval { get; set; }  // "day", "week", "month", or "year"
    public long IntervalCount { get; set; } // e.g., every 3 months
}
