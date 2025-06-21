using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Track;

[ExtendObjectType(typeof(Tracks))]
public class TracksResolver
{
    public async Task<Artists?> GetArtistAsync(
        [Parent] Tracks track,
        ArtistByIdDataLoader artistByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await artistByIdDataLoader.LoadAsync(track.ArtistId, cancellationToken);
    }
}
