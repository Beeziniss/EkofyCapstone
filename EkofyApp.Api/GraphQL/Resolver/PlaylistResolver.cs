using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Playlist))]
public sealed class PlaylistResolver
{
    public async Task<User?> GetUserAsync(
        [Parent] Playlist playlist,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await userByIdDataLoader.LoadAsync(playlist.UserId, cancellationToken);
    }

    public async Task<IEnumerable<Track?>> GetTracksAsync(
        [Parent] Playlist playlist,
        DataLoaderCustomOneToOne<Track> trackByIdDataLoader,
        CancellationToken cancellationToken)
    {
        List<string> trackIds = playlist.TracksInfo.Select(t => t.TrackId).ToList();
        return await trackByIdDataLoader.LoadAsync(trackIds, cancellationToken) ?? [];
    }
}
