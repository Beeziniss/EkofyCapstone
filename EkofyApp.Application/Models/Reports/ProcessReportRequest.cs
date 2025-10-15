using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Reports;

namespace EkofyApp.Application.Models.Reports;

/// <summary>
/// Request ?? moderator x? lý báo cáo
/// </summary>
public sealed record class ProcessReportRequest
{
    /// <summary>
    /// ID báo cáo
    /// </summary>
    public string ReportId { get; init; } = null!;

    /// <summary>
    /// Tr?ng thái m?i
    /// </summary>
    public ReportStatus Status { get; init; }

    /// <summary>
    /// Hành ??ng th?c hi?n
    /// </summary>
    public ReportAction ActionTaken { get; init; }

    public List<RestrictionActionDetail> RestrictionActionDetails { get; init; } = [];

    /// <summary>
    /// S? ngày suspend (n?u là Suspended)
    /// </summary>
    public int? SuspensionDays { get; init; }

    public string? Note { get; init; }
}
