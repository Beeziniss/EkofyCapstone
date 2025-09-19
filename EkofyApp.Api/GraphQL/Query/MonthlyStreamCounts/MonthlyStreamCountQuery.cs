using EkofyApp.Application.ServiceInterfaces.MonthlyStreamCounts;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.MonthlyStreamCounts;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class MonthlyStreamCountQuery(IMonthlyStreamCountService monthlyStreamCountService)
{
    private readonly IMonthlyStreamCountService _monthlyStreamCountService = monthlyStreamCountService;

    public IQueryable<MonthlyStreamCount> GetMonthlyStreamCounts()
    {
        return _monthlyStreamCountService.GetMonthlyStreamCounts();
    }
}
