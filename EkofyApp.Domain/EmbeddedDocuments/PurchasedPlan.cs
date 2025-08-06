using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class PurchasedPlan
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SubscriptionId { get; set; }

    public DateTime? BuyedTime { get; set; } // Thời điểm mua
    public DateTime? ExpiredTime { get; set; } // Thời điểm hết hạn
}

