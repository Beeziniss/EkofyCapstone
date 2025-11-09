using EkofyApp.Application.ServiceInterfaces.Revenues;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Revenues;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ArtistRevenueQuery(IArtistRevenueService artistRevenueService)
{
    private readonly IArtistRevenueService _artistRevenueService = artistRevenueService;

    [AllowAnonymous]
    [UseProjection]
    [UseFiltering]
    [UseSorting<ArtistRevenue>]
    public IQueryable<ArtistRevenue> GetArtistRevenues()
    {
        return _artistRevenueService.GetArtistRevenues();
    }
}
