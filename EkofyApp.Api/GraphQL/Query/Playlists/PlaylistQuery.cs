using EkofyApp.Application.ServiceInterfaces.Playlists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Playlists;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class PlaylistQuery(IPlaylistService playlistService)
{
    private readonly IPlaylistService _playlistService = playlistService;

    public IQueryable<Playlist> GetPlaylists()
    {
        return _playlistService.GetPlaylists();
    }
}
