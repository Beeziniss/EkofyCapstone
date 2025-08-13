using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Artists;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ArtistQuery(IArtistService artistService)
{
    private readonly IArtistService _artistService = artistService;

    public IQueryable<Artist> GetArtists()
    {
        return _artistService.GetArtistsQueryable();
    }
}
