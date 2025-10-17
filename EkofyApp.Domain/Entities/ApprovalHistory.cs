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
    public string TargetId { get; set; } = null!; // e.g. userId, trackId
    public ApprovalType ApprovalType { get; set; } // e.g. "Artist", "Track", "Album"

    [BsonRepresentation(BsonType.ObjectId)]
    public string ApprovedByUserId { get; set; } = null!;
    public string ApprovedByName { get; set; } = null!;

    public DateTimeOffset ApprovedAt { get; set; }
    public string Action { get; set; } = null!; // "Approve", "Reject", "RequestChange", etc.
    public string? Notes { get; set; }

    // Lưu lại bản đăng ký/bản ghi trước khi duyệt (JSON hoặc object)
    [BsonRepresentation(BsonType.Document)]
    public object Snapshot { get; set; } = null!;
}
