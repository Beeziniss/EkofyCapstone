using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class PurchasedPlan
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SubscriptionId { get; set; }

    public DateTimeOffset? BuyedTime { get; set; } // Thời điểm mua
    public DateTimeOffset? ExpiredTime { get; set; } // Thời điểm hết hạn
}

