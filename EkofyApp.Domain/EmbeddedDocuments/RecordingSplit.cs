using EkofyApp.Domain.Enums.Artist;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class RecordingSplit
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user associated with the split
    public ArtistRole ArtistRole { get; set; } // UserRole of the artist in the recording, e.g., Performer, Producer, etc.
    public decimal Percentage { get; set; } = default; // Percentage of the recording split, e.g., 50.0 for 50%
}
