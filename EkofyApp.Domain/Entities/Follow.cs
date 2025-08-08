using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Follow : IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the follow relationship
    [BsonRepresentation(BsonType.ObjectId)]
    public string FollowerId { get; set; } = null!; // Unique identifier for the user who is following
    [BsonRepresentation(BsonType.ObjectId)]
    public string FollowedId { get; set; } = null!; // Unique identifier for the user being followed
    public DateTime CreatedAt { get; set; } // Timestamp when the follow relationship was created
}
