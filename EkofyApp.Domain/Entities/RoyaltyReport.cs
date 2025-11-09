using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class RoyaltyReport // Snapshot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!;

    public int Month { get; set; }
    public int Year { get; set; }

    public long StreamCount { get; set; }
    public decimal TotalRoyaltyAmount { get; set; }

    public List<RoyaltySplit> RoyaltySplits { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset();
}
