using EkofyApp.Application.Models.Reports;
using EkofyApp.Application.ServiceInterfaces.Reports;

namespace EkofyApp.Api.GraphQL.Mutation.Reports;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class ReportMutation(IReportService reportService)
{
    private readonly IReportService _reportService = reportService;

    public async Task<bool> CreateReportAsync(CreateReportRequest request)
    {
        await _reportService.CreateReportAsync(request);
        return true;
    }

    public async Task<bool> AssignReportToModeratorAsync(string reportId, string moderatorId)
    {
        await _reportService.AssignReportToModeratorAsync(reportId, moderatorId);
        return true;
    }

    public async Task<bool> ProcessReportAsync(ProcessReportRequest request)
    {
        await _reportService.ProcessReportAsync(request);
        return true;
    }

    ///// <summary>
    ///// Update priority c?a báo cáo
    ///// </summary>
    //public async Task<bool> UpdateReportPriorityAsync(string reportId, string priority)
    //{
    //    return await _reportService.UpdateReportPriorityAsync(reportId, priority);
    //}

    ///// <summary>
    ///// Xóa báo cáo (soft delete)
    ///// </summary>
    //public async Task<bool> DeleteReportAsync(string reportId)
    //{
    //    return await _reportService.DeleteReportAsync(reportId);
    //}

    ///// <summary>
    ///// Escalate báo cáo lên admin
    ///// </summary>
    //public async Task<bool> EscalateReportAsync(string reportId)
    //{
    //    return await _reportService.EscalateReportAsync(reportId);
    //}
}
