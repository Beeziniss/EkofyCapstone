using EkofyApp.Domain.Enums.Artist;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Application.Models.Projections;
[BsonIgnoreExtraElements]
public sealed class RecordingProjection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the recording
    //public string ISRC { get; set; } = null!; // International Standard RecordingProjection Code (ISRC) for the recording
    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!; // Reference to the associated track

    public string? Description { get; set; } // PackageDescription of the recording, if available
    public List<RecordingSplitProjection> RecordingSplits { get; set; } = []; // List of splits for the recording, e.g., 50% to Artist A, 50% to Artist B
}

[BsonIgnoreExtraElements]
public sealed class RecordingSplitProjection
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user associated with the split
    public ArtistRole ArtistRole { get; set; } // UserRole of the artist in the recording, e.g., Performer, Producer, etc.
    public decimal Percentage { get; set; } = default; // Percentage of the recording split, e.g., 50.0 for 50%
}
