using EkofyApp.Domain.Enums.Reports;

namespace EkofyApp.Application.Models.Reports;

public sealed class CreateReportRequest
{
    public string ReportedUserId { get; set; } = null!;

    public ReportType ReportType { get; set; }

    public string Description { get; set; } = null!;

    public string? RelatedContentId { get; set; }

    public ReportRelatedContentType? RelatedContentType { get; set; }

    public List<string>? Evidences { get; set; }
}
