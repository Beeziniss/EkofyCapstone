using EkofyApp.Application.Models.Playlists;
using EkofyApp.Application.ServiceInterfaces.Playlists;

namespace EkofyApp.Api.GraphQL.Mutation.Playlists;


[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class PlaylistMutation(IPlaylistService playlistService)
{
    private readonly IPlaylistService _playlistService = playlistService;

    public async Task<bool> CreatePlaylistAsync(CreatePlaylistRequest createPlaylistRequest)
    {
        await _playlistService.CreatePlaylistAsync(createPlaylistRequest);
        return true;
    }

    public async Task<bool> UpdatePlaylistAsync(UpdatePlaylistRequest updatePlaylistRequest)
    {
        await _playlistService.UpdatePlaylistAsync(updatePlaylistRequest);
        return true;
    }

    public async Task<bool> AddToFavoriteAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        await _playlistService.AddToFavoriteAsync(addToPlaylistRequest);
        return true;
    }

    public async Task<bool> AddToPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        await _playlistService.AddToPlaylistAsync(addToPlaylistRequest);
        return true;
    }

    public async Task<bool> RemoveFromPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        await _playlistService.RemoveFromPlaylistAsync(addToPlaylistRequest);
        return true;
    }

    public async Task<bool> DeletePlaylistAsync(string playlistId)
    {
        await _playlistService.DeletePlaylistAsync(playlistId);
        return true;
    }
}
