using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class UserEngagement
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the follow relationship

    [BsonRepresentation(BsonType.ObjectId)]
    public string ActorId { get; set; } = null!; // Unique identifier for the user who is following
    public UserEngagementTargetType ActorType { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string TargetId { get; set; } = null!; // Unique identifier for the user being followed
    public UserEngagementTargetType TargetType { get; set; } // Type of the followed entity, e.g., "artist", "podcast", etc.

    public UserEngagementAction Action { get; set; } // Follow, Like, Bookmark

    public DateTimeOffset CreatedAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset(); // Timestamp when the follow relationship was created
}
