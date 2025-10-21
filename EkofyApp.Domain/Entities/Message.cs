using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the message

    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = null!; // Unique identifier for the conversation this message belongs to

    [BsonRepresentation(BsonType.ObjectId)]
    public string SenderId { get; set; } = null!; // Unique identifier for the sender of the message

    [BsonRepresentation(BsonType.ObjectId)]
    public string ReceiverId { get; set; } = null!; // Unique identifier for the receiver of the message

    public string Text { get; set; } = null!;

    public bool IsRead { get; set; } = false;

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> DeletedForIds { get; set; } = [];

    public DateTimeOffset SentAt { get; set; }

    //[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    //public DateTimeOffset ExpireAt { get; set; } = DateTime.UtcNow.AddDays(30);
}
