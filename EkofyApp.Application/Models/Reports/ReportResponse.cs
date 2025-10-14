using EkofyApp.Domain.Enums.Reports;

namespace EkofyApp.Application.Models.Reports;

/// <summary>
/// Response cho báo cáo
/// </summary>
public sealed class ReportResponse
{
    public string Id { get; set; } = null!;
    public string ReportedUserId { get; set; } = null!;
    public string ReportedUserName { get; set; } = null!;
    public string ReporterId { get; set; } = null!;
    public string ReporterName { get; set; } = null!;
    public ReportType ReportType { get; set; }
    public string Description { get; set; } = null!;
    public ReportStatus Status { get; set; }
    public ReportPriority Priority { get; set; }
    public string? RelatedContentId { get; set; }
    public string? RelatedContentType { get; set; }
    public List<string> EvidenceUrls { get; set; } = [];
    public string? AssignedModeratorId { get; set; }
    public string? AssignedModeratorName { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public ReportAction? ActionTaken { get; set; }
    public string? ModeratorNotes { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public int TotalReportsCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
