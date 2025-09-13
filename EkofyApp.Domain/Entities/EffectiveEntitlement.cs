using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Users;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class EffectiveEntitlement : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    // For audit/debug
    [BsonRepresentation(BsonType.ObjectId)]
    public string SubscriptionId { get; set; } = null!; // Unique identifier for the subscription plan

    public UserRole Role { get; set; }

    //// Embedded subscription information
    //public string? SubscriptionCode { get; set; }
    //public int SubscriptionVersion { get; set; }

    public List<Entitlement> Entitlements { get; set; } = [];
    public DateTimeOffset? ValidUntil { get; set; }
}
