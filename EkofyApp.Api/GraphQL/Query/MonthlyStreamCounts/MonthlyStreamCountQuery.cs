using EkofyApp.Application.ServiceInterfaces.MonthlyStreamCounts;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.MonthlyStreamCounts;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class MonthlyStreamCountQuery(IMonthlyStreamCountService monthlyStreamCountService)
{
    private readonly IMonthlyStreamCountService _monthlyStreamCountService = monthlyStreamCountService;

    [AuthorizeRoles(HelperRoleBase.ArtistModeratorAdminRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<MonthlyStreamCount>]
    public IQueryable<MonthlyStreamCount> GetMonthlyStreamCounts()
    {
        return _monthlyStreamCountService.GetMonthlyStreamCounts();
    }
}
