using EkofyApp.Domain.Enums.Artist;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Application.Models.Projections;
[BsonIgnoreExtraElements]
public sealed class WorkProjection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the work
    //public string ISWC { get; set; } = null!; // International Standard Musical WorkProjection Code (ISWC) for the work
    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!; // Reference to the associated track

    public string? Description { get; set; } // Description of the work, if available

    public List<WorkSplitProjection> WorkSplits { get; set; } = []; // List of splits for the work, e.g., 50% to Artist A, 50% to Artist B
}

[BsonIgnoreExtraElements]
public sealed class WorkSplitProjection
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the work split
    public ArtistRole ArtistRole { get; set; } // UserRole of the artist in the work, e.g., Composer, Lyricist, etc.
    public decimal Percentage { get; set; } = default; // Percentage of the work split, e.g., 50.0 for 50%
}
