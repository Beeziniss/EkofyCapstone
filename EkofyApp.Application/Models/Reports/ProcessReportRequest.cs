using EkofyApp.Domain.Enums.Reports;

namespace EkofyApp.Application.Models.Reports;

/// <summary>
/// Request ?? moderator x? lý báo cáo
/// </summary>
public sealed class ProcessReportRequest
{
    /// <summary>
    /// ID báo cáo
    /// </summary>
    public string ReportId { get; set; } = null!;

    /// <summary>
    /// Tr?ng thái m?i
    /// </summary>
    public ReportStatus Status { get; set; }

    /// <summary>
    /// Hành ??ng th?c hi?n
    /// </summary>
    public ReportAction ActionTaken { get; set; }

    /// <summary>
    /// S? ngày suspend (n?u là TemporarySuspension)
    /// </summary>
    public int? SuspensionDays { get; set; }

    /// <summary>
    /// Ghi chú c?a moderator
    /// </summary>
    public string? ModeratorNotes { get; set; }
}
