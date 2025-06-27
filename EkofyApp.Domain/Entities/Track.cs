using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities
{
    public class Track
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        public string? Description { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ArtistId { get; set; }
    }
}
