using EkofyApp.Api.GraphQL;
using EkofyApp.Application.Models.Reports;
using EkofyApp.Application.ServiceInterfaces.Reports;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Reports;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ReportQuery(IReportService reportService)
{
    private readonly IReportService _reportService = reportService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Report>]
    public IQueryable<Report> GetReports()
    {
        return _reportService.GetReports();
    }

    /// <summary>
    /// L?y th?ng kê v? reports - ch? dành cho Moderator và Admin
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.ModeratorAdminRoles)]
    public async Task<ReportStatisticsResponse> GetReportStatisticsAsync()
    {
        return await _reportService.GetReportStatisticsAsync();
    }
}
