using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities
{
    public sealed class RequestHub : IEntityCustom
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!; // Unique identifier for the recording

        public string Title { get; set; }
        public string Description { get; set; }
        public List<string>? Attachments { get; set; }
        public bool IsClosed { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
