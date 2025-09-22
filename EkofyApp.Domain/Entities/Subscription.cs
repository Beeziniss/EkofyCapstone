using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
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
    public int Version { get; init; } // Version of the subscription, default is 1

    public decimal Amount { get; set; }
    public CurrencyType Currency { get; set; } = CurrencyType.vnd; // Default currency is vnd

    public SubscriptionTier Tier { get; set; } // TODO: Cân nhắc có nên embed không
    public SubscriptionStatus Status { get; set; }
}
