using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class RoyaltyReport : IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? RecordingId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? WorkId { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public long StreamCount { get; set; }
    public decimal TotalRoyaltyAmount { get; set; } // Tổng tiền bản quyền được tạo ra

    public List<RoyaltySplit> RoyaltySplits { get; set; } = [];
}
