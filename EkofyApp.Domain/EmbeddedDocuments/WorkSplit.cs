using EkofyApp.Domain.Enums.Artist;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class WorkSplit
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the work split
    public ArtistRole ArtistRole { get; set; } // Role of the artist in the work, e.g., Composer, Lyricist, etc.
    public decimal Percentage { get; set; } = default; // Percentage of the work split, e.g., 50.0 for 50%
}