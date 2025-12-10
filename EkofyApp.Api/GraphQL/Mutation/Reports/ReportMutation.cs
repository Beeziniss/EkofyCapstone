using EkofyApp.Api.GraphQL;
using EkofyApp.Application.Models.Reports;
using EkofyApp.Application.ServiceInterfaces.Reports;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Reports;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class ReportMutation(IReportService reportService, IUserService userService)
{
    private readonly IReportService _reportService = reportService;
    private readonly IUserService _userService = userService;

    public async Task<bool> CreateReportAsync(CreateReportRequest request)
    {
        bool hasAnyRestriction = await _userService.CheckMultipleRestrictionsAsync(RestrictionAction.Report);
        if (hasAnyRestriction)
        {
            throw new UnauthorizedCustomException("You are restricted from reporting.");
        }

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

    public async Task<bool> UpdateReportPriorityAsync(string reportId, string priority)
    {
        return await _reportService.UpdateReportPriorityAsync(reportId, priority);
    }

    public async Task<bool> DeleteReportAsync(string reportId)
    {
        return await _reportService.DeleteReportAsync(reportId);
    }

    public async Task<bool> EscalateReportAsync(string reportId)
    {
        return await _reportService.EscalateReportAsync(reportId);
    }

    public async Task<bool> RestoreUserAsync(string reportId)
    {
        return await _reportService.UnbanUserAsync(reportId);
    }

    public async Task<bool> RestoreContentAsync(string reportId)
    {
        await _reportService.RestoreContentAsync(reportId);
        return true;
    }
}
