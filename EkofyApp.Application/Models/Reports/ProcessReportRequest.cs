using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Reports;

namespace EkofyApp.Application.Models.Reports;

/// <summary>
/// Requests ?? moderator x? lý báo cáo
/// </summary>
public sealed record class ProcessReportRequest
{
    public string ReportId { get; init; } = null!;

    public ReportStatus Status { get; init; }

    public ReportAction ActionTaken { get; init; }

    public List<RestrictionActionDetail> RestrictionActionDetails { get; init; } = [];

    public int? SuspensionDays { get; init; }

    public string? Note { get; init; }
}
