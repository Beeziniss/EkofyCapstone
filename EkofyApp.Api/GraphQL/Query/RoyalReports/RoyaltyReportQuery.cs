using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.RoyalReports;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class RoyaltyReportQuery(IRoyaltyReportService royaltyReportService)
{
    private readonly IRoyaltyReportService _royaltyReportService = royaltyReportService;

    public IQueryable<RoyaltyReport> GetRoyaltyReports()
    {
        return _royaltyReportService.GetRoyaltyReports();
    }
}
