using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;

public sealed class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string ActorId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TargetId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? RelatedId { get; set; } // e.g., related trackId, albumId, etc.
    public NotificationRelatedType? RelatedType { get; set; }

    public string Content { get; set; } = null!;

    public string? Url { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTimeOffset? ReadAt { get; set; }

    public NotificationActionType Action { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset();
}
