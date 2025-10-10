using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.RoyalReports;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class RoyaltyReportQuery(IRoyaltyReportService royaltyReportService)
{
    private readonly IRoyaltyReportService _royaltyReportService = royaltyReportService;

    [AuthorizeRoles(HelperRoleBase.ArtistModeratorAdminRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<RoyaltyReport>]
    public IQueryable<RoyaltyReport> GetRoyaltyReports()
    {
        return _royaltyReportService.GetRoyaltyReports();
    }
}
