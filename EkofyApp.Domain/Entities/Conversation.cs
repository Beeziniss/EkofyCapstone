using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Conversation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the conversation

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> UserIds { get; set; } = []; // Exactly 2 users

    public LastMessage? LastMessage { get; set; }

    public List<DeletedForEntry> DeletedFor { get; set; } = []; // userId => true if deleted

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public sealed class DeletedForEntry
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user who deleted the conversation

    public bool IsDeleted { get; set; }
}

public sealed class LastMessage
{
    public string Text { get; set; } = null!; // Text of the last message

    [BsonRepresentation(BsonType.ObjectId)]
    public string SenderId { get; set; } = null!; // Unique identifier for the sender of the last message

    public DateTime SentAt { get; set; }

    public List<string> IsReadBy { get; set; } = [];
}
