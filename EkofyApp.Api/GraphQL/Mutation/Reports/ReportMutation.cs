using EkofyApp.Application.Models.Reports;
using EkofyApp.Application.ServiceInterfaces.Reports;

namespace EkofyApp.Api.GraphQL.Mutation.Reports;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class ReportMutation(IUserReportService reportService)
{
    private readonly IUserReportService _reportService = reportService;

    /// <summary>
    /// T?o báo cáo vi ph?m m?i
    /// </summary>
    public async Task<ReportResponse> CreateReportAsync(CreateReportRequest request)
    {
        return await _reportService.CreateReportAsync(request);
    }

    /// <summary>
    /// Assign báo cáo cho moderator
    /// </summary>
    public async Task<bool> AssignReportToModeratorAsync(string reportId, string moderatorId)
    {
        return await _reportService.AssignReportToModeratorAsync(reportId, moderatorId);
    }

    /// <summary>
    /// Moderator x? lý báo cáo
    /// </summary>
    public async Task<ReportResponse> ProcessReportAsync(ProcessReportRequest request)
    {
        return await _reportService.ProcessReportAsync(request);
    }

    /// <summary>
    /// Update priority c?a báo cáo
    /// </summary>
    public async Task<bool> UpdateReportPriorityAsync(string reportId, string priority)
    {
        return await _reportService.UpdateReportPriorityAsync(reportId, priority);
    }

    /// <summary>
    /// Xóa báo cáo (soft delete)
    /// </summary>
    public async Task<bool> DeleteReportAsync(string reportId)
    {
        return await _reportService.DeleteReportAsync(reportId);
    }

    /// <summary>
    /// Escalate báo cáo lên admin
    /// </summary>
    public async Task<bool> EscalateReportAsync(string reportId)
    {
        return await _reportService.EscalateReportAsync(reportId);
    }
}
