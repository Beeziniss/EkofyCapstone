using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Subcriptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Subscription : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Code { get; set; } = null!; // Unique code for the subscription
    public int Version { get; init; } = 1; // Version of the subscription, default is 1

    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND"; // Default currency is USD

    public SubscriptionTier Tier { get; set; } // Cân nhắc có nên embed không
    public List<Entitlement> Entitlements { get; set; } = [];
}
