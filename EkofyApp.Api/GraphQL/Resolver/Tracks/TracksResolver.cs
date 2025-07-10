using EkofyApp.Api.GraphQL.DataLoader.Artists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver.Tracks;

[ExtendObjectType(typeof(Track))]
public class TracksResolver
{
    public async Task<Artist?> GetArtistAsync(
        [Parent] Track track,
        ArtistDataLoader artistByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await artistByIdDataLoader.LoadAsync(track.ArtistId, cancellationToken);
    }
}
