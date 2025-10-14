using EkofyApp.Application.Models.Reports;
using EkofyApp.Application.ServiceInterfaces.Reports;

namespace EkofyApp.Api.GraphQL.Query.Reports;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ReportQuery(IUserReportService reportService)
{
    private readonly IUserReportService _reportService = reportService;

    /// <summary>
    /// L?y danh sách báo cáo (v?i filter và pagination)
    /// </summary>
    public async Task<ReportListResponse> GetReportsAsync(GetReportsRequest request)
    {
        return await _reportService.GetReportsAsync(request);
    }

    /// <summary>
    /// L?y chi ti?t m?t báo cáo
    /// </summary>
    public async Task<ReportResponse> GetReportByIdAsync(string reportId)
    {
        return await _reportService.GetReportByIdAsync(reportId);
    }

    /// <summary>
    /// L?y t?t c? báo cáo v? m?t user
    /// </summary>
    public async Task<List<ReportResponse>> GetReportsByUserIdAsync(string userId)
    {
        return await _reportService.GetReportsByUserIdAsync(userId);
    }

    /// <summary>
    /// L?y statistics v? reports
    /// </summary>
    public async Task<ReportStatisticsResponse> GetReportStatisticsAsync()
    {
        return await _reportService.GetReportStatisticsAsync();
    }
}
