using EkofyApp.Application.Models.Reports;

namespace EkofyApp.Application.ServiceInterfaces.Reports;

public interface IUserReportService
{
    /// <summary>
    /// T?o báo cáo vi ph?m m?i
    /// </summary>
    Task<ReportResponse> CreateReportAsync(CreateReportRequest request);

    /// <summary>
    /// L?y danh sách báo cáo (v?i filter và pagination)
    /// </summary>
    Task<ReportListResponse> GetReportsAsync(GetReportsRequest request);

    /// <summary>
    /// L?y chi ti?t m?t báo cáo
    /// </summary>
    Task<ReportResponse> GetReportByIdAsync(string reportId);

    /// <summary>
    /// Assign báo cáo cho moderator
    /// </summary>
    Task<bool> AssignReportToModeratorAsync(string reportId, string moderatorId);

    /// <summary>
    /// Moderator x? lý báo cáo
    /// </summary>
    Task<ReportResponse> ProcessReportAsync(ProcessReportRequest request);

    /// <summary>
    /// L?y t?t c? báo cáo v? m?t user
    /// </summary>
    Task<List<ReportResponse>> GetReportsByUserIdAsync(string userId);

    /// <summary>
    /// L?y statistics v? reports
    /// </summary>
    Task<ReportStatisticsResponse> GetReportStatisticsAsync();

    /// <summary>
    /// Update priority c?a báo cáo
    /// </summary>
    Task<bool> UpdateReportPriorityAsync(string reportId, string priority);

    /// <summary>
    /// Xóa báo cáo (soft delete)
    /// </summary>
    Task<bool> DeleteReportAsync(string reportId);

    /// <summary>
    /// Escalate báo cáo lên admin
    /// </summary>
    Task<bool> EscalateReportAsync(string reportId);
}
