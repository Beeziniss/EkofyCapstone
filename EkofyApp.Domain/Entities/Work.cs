using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Work : IEntityCustom // TODO: Chưa xong hết các trường cần thiết
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the work
    //public string ISWC { get; set; } = null!; // International Standard Musical Work Code (ISWC) for the work
    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!; // Reference to the associated track

    public string? Description { get; set; } // Description of the work, if available

    public List<WorkSplit> WorkSplits { get; set; } = []; // List of splits for the work, e.g., 50% to Artist A, 50% to Artist B

    public long Version { get; set; } // Version of the work, incremented on each update
    public WorkStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset(); // Timestamp when the work was created
}
