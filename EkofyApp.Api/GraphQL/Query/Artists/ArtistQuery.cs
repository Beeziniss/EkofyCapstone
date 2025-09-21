using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Artists;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ArtistQuery(IArtistService artistService)
{
    private readonly IArtistService _artistService = artistService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Artist>]
    public IQueryable<Artist> GetArtists()
    {
        return _artistService.GetArtistsQueryable();
    }
}
