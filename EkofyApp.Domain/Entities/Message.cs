using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public class Message
{
    [BsonId]
    public string Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string SenderId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string ReceiverId { get; set; }

    public string Text { get; set; }

    public bool IsRead { get; set; } = false;

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> DeletedFor { get; set; } = [];

    public DateTime SentAt { get; set; }

    //[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    //public DateTime ExpireAt { get; set; } = DateTime.UtcNow.AddDays(30);
}
