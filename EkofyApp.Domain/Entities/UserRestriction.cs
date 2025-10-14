using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Reports;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;

/// <summary>
/// L?ch s? các hành ??ng x? ph?t user
/// </summary>
public sealed class UserRestriction : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// User b? x? ph?t
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Lo?i h?n ch?
    /// </summary>
    public RestrictionType RestrictionType { get; set; }

    /// <summary>
    /// Lý do h?n ch?
    /// </summary>
    public string Reason { get; set; } = null!;

    /// <summary>
    /// ID báo cáo liên quan
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ReportId { get; set; }

    /// <summary>
    /// Moderator th?c hi?n hành ??ng
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string ModeratorId { get; set; } = null!;

    /// <summary>
    /// Hành ??ng ?ã th?c hi?n
    /// </summary>
    public ReportAction ActionType { get; set; }

    /// <summary>
    /// Th?i gian b?t ??u h?n ch?
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Th?i gian k?t thúc h?n ch? (null n?u permanent)
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// S? ngày b? suspend (n?u là temporary suspension)
    /// </summary>
    public int? DurationDays { get; set; }

    /// <summary>
    /// Có ?ang active hay không
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ghi chú c?a moderator
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Thông tin b? sung
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
