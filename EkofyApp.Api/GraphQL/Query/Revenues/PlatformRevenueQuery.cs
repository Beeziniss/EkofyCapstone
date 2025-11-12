using EkofyApp.Application.ServiceInterfaces.Revenues;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Revenues;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class PlatformRevenueQuery(IPlatformRevenueService platformRevenueService)
{
    private readonly IPlatformRevenueService _platformRevenueService = platformRevenueService;

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<PlatformRevenue>]
    public IQueryable<PlatformRevenue> GetPlatformRevenues()
    {
        return _platformRevenueService.GetPlatformRevenues();
    }
}
