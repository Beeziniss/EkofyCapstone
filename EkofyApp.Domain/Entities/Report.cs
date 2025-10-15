using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums.Reports;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;

/// <summary>
/// Báo cáo vi ph?m c?a user
/// </summary>
public sealed class Report : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// User b? báo cáo
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string ReportedUserId { get; set; } = null!;

    /// <summary>
    /// User th?c hi?n báo cáo (reporter)
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string ReporterId { get; set; } = null!;

    /// <summary>
    /// Lo?i vi ph?m
    /// </summary>
    public ReportType ReportType { get; set; }

    /// <summary>
    /// Mô t? chi ti?t vi ph?m
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Tr?ng thái x? lý
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    /// <summary>
    /// M?c ?? ?u tiên
    /// </summary>
    public ReportPriority Priority { get; set; } = ReportPriority.Medium;

    /// <summary>
    /// ID c?a n?i dung vi ph?m (track, comment, playlist, etc.)
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RelatedContentId { get; set; }

    /// <summary>
    /// Lo?i n?i dung vi ph?m (Track, Comment, Playlist, Profile, etc.)
    /// </summary>
    public string? RelatedContentType { get; set; }

    /// <summary>
    /// URL ho?c screenshot b?ng ch?ng
    /// </summary>
    public List<string> Evidences { get; set; } = [];

    /// <summary>
    /// Moderator ?ang x? lý
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedModeratorId { get; set; }

    /// <summary>
    /// Hành ??ng ?ã th?c hi?n
    /// </summary>
    public ReportAction? ActionTaken { get; set; }

    /// <summary>
    /// Ghi chú c?a moderator
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Th?i gian hoàn thành x? lý
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// S? l?n user này b? báo cáo (t? ??ng tính)
    /// </summary>
    public int TotalReportsCount { get; set; } = 1;

    /// <summary>
    /// ?ánh d?u ?ã xóa (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
