using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities
{
    public class RequestComment :IEntityCustom
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!; // Unique identifier for the recording

        public string RequestId { get; set; }
        public string CommentatorId { get; set; }
        public string Content { get; set; }
    }
}
