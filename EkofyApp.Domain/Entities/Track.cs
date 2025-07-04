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
        public List<string> CategoryIds { get; set; } = []; // List of category IDs this track belongs to
        public List<string> Tags { get; set; } // e.g., "music", "podcast", etc.

        [BsonRepresentation(BsonType.ObjectId)]
        public string ArtistId { get; set; }

        public AudioFeature AudioFeature { get; set; } // Contains audio features
        public AudioFingerprint AudioFingerprint { get; set; } // Unique identifier for the audio content

        public bool IsPublished { get; set; } = false; // Indicates if the track is public or private
        public bool IsApproved { get; set; } = false; // Indicates if the track is visible to users

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
