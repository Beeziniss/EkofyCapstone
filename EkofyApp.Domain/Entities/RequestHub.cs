using EkofyApp.Domain.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities
{
    public sealed class RequestHub : TimeStamped, IEntityCustom
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!; // Unique identifier for the recording

        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public List<string>? Attachments { get; set; }
        public bool IsClosed { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        public bool IsVisible { get; set; } = true; // Indicates if it is visible to users
    }
}
