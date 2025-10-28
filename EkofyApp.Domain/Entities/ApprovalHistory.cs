using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class ApprovalHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? TargetOwnerId { get; set; } // e.g. owner of the track/album
    [BsonRepresentation(BsonType.ObjectId)]
    public string TargetId { get; set; } = null!; // e.g. userId, trackId
    public ApprovalType ApprovalType { get; set; } // e.g. "Artist", "Track", "Album"

    [BsonRepresentation(BsonType.ObjectId)]
    public string ApprovedByUserId { get; set; } = null!;

    public DateTimeOffset ActionAt { get; set; }
    public HistoryActionType Action { get; set; } // "Approve", "Reject", "RequestChange", etc.
    public string? Notes { get; set; }

    // Lưu lại bản đăng ký/bản ghi trước khi duyệt (JSON hoặc object)
    public string Snapshot { get; set; } = null!;
}
