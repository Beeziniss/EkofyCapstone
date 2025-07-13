using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Artists;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class ArtistQuery(IArtistService artistService, IArtistGraphQLService artistGraphQLService)
{
    private readonly IArtistService _artistService = artistService;
    private readonly IArtistGraphQLService _artistGraphQLService = artistGraphQLService;

    public IQueryable<Artist> GetArtists()
    {
        return _artistGraphQLService.GetArtistsQueryable();
    }
}
