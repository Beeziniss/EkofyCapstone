using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums.Reports;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;

public sealed class Report : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string ReportedUserId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string ReporterId { get; set; } = null!;

    public ReportType ReportType { get; set; }

    public string Description { get; set; } = null!;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public ReportPriority Priority { get; set; } = ReportPriority.Medium;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? RelatedContentId { get; set; }

    public ReportRelatedContentType? RelatedContentType { get; set; }

    public List<string> Evidences { get; set; } = [];

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedModeratorId { get; set; }

    public ReportAction? ActionTaken { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public long TotalReportsCount { get; set; } = 1;

    public bool IsDeleted { get; set; } = false;
}
