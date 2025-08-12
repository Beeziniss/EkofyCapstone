using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class EffectiveFeature
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    // For audit/debug
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SubscriptionId { get; set; }

    public UserRole Role { get; set; }

    // Embedded subscription information
    public string? SubscriptionCode { get; set; }
    public int SubscriptionVersion { get; set; }

    public List<string> FeatureCodes { get; set; } = [];
    public DateTime ValidUntil { get; set; }
}
