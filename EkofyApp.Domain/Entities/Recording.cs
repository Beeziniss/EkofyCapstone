using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Recording : IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the recording
    //public string ISRC { get; set; } = null!; // International Standard Recording Code (ISRC) for the recording
    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!; // Reference to the associated track

    public string? Description { get; set; } // PackageDescription of the recording, if available
    public List<RecordingSplit> RecordingSplits { get; set; } = []; // List of splits for the recording, e.g., 50% to Artist A, 50% to Artist B

    public long Version { get; set; } // Version of the work, incremented on each update
    public RecordingStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset(); // Timestamp when the recording was created
}
