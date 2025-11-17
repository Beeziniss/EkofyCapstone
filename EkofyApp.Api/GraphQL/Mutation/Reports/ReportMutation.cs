using EkofyApp.Api.GraphQL;
using EkofyApp.Application.Models.Reports;
using EkofyApp.Application.ServiceInterfaces.Reports;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Reports;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class ReportMutation(IReportService reportService)
{
    private readonly IReportService _reportService = reportService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    public async Task<bool> CreateReportAsync(CreateReportRequest request)
    {
        await _reportService.CreateReportAsync(request);
        return true;
    }

    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    public async Task<bool> AssignReportToModeratorAsync(string reportId, string moderatorId)
    {
        await _reportService.AssignReportToModeratorAsync(reportId, moderatorId);
        return true;
    }

    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    public async Task<bool> ProcessReportAsync(ProcessReportRequest request)
    {
        await _reportService.ProcessReportAsync(request);
        return true;
    }

    /// <summary>
    /// Update priority c?a báo cáo
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    public async Task<bool> UpdateReportPriorityAsync(string reportId, string priority)
    {
        return await _reportService.UpdateReportPriorityAsync(reportId, priority);
    }

    /// <summary>
    /// Xóa báo cáo (soft delete)
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    public async Task<bool> DeleteReportAsync(string reportId)
    {
        return await _reportService.DeleteReportAsync(reportId);
    }

    /// <summary>
    /// Escalate báo cáo lên admin
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    public async Task<bool> EscalateReportAsync(string reportId)
    {
        return await _reportService.EscalateReportAsync(reportId);
    }

    /// <summary>
    /// Khôi ph?c user kh?i permanent ban d?a trên reportId
    /// Ch? admin m?i có quy?n th?c hi?n
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    public async Task<bool> RestoreUserAsync(string reportId)
    {
        return await _reportService.UnbanUserAsync(reportId);
    }

    /// <summary>
    /// Khôi ph?c content ?ã b? g? do báo cáo
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    public async Task<bool> RestoreContentAsync(string reportId)
    {
        await _reportService.RestoreContentAsync(reportId);
        return true;
    }
}
