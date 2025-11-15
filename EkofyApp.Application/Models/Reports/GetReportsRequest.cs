using EkofyApp.Domain.Enums.Reports;

namespace EkofyApp.Application.Models.Reports;

/// <summary>
/// Requests ?? l?y danh sách báo cáo (v?i filter)
/// </summary>
public sealed class GetReportsRequest
{
    /// <summary>
    /// Filter theo tr?ng thái
    /// </summary>
    public ReportStatus? Status { get; set; }

    /// <summary>
    /// Filter theo lo?i báo cáo
    /// </summary>
    public ReportType? ReportType { get; set; }

    /// <summary>
    /// Filter theo priority
    /// </summary>
    public ReportPriority? Priority { get; set; }

    /// <summary>
    /// Filter theo user b? báo cáo
    /// </summary>
    public string? ReportedUserId { get; set; }

    /// <summary>
    /// Filter theo moderator ???c assign
    /// </summary>
    public string? AssignedModeratorId { get; set; }

    /// <summary>
    /// T? ngày
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// ??n ngày
    /// </summary>
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Page number
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;
}
