using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Application.Models.Projections;
[BsonIgnoreExtraElements]
public sealed class MonthlyStreamCountProjection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!;

    public int Month { get; set; } // ví dụ: 9
    public int Year { get; set; }  // ví dụ: 2025

    public long StreamCount { get; set; } = 0; // Tổng số lượt stream

    public DateTimeOffset CreatedAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset(); // Thời gian tạo bản ghi
    public DateTimeOffset? ProcessedAt { get; set; } // Thời gian xử lý cuối cùng

    public RecordingProjection? RecordingProjection { get; set; } = default!;
    public WorkProjection? WorkProjection { get; set; } = default!;
}
