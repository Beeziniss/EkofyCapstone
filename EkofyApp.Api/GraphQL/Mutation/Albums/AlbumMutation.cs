using EkofyApp.Application.Models.Albums;
using EkofyApp.Application.ServiceInterfaces.Albums;

namespace EkofyApp.Api.GraphQL.Mutation.Albums;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class AlbumMutation(IAlbumService albumService)
{
    private readonly IAlbumService _albumService = albumService;

    public async Task<bool> CreateAlbumAsync(CreateAlbumRequest createAlbumRequest)
    {
        await _albumService.CreateAlbumAsync(createAlbumRequest);
        return true;
    }

    public async Task<bool> AddToFavoriteAlbumAsync(string albumId, bool isAdding)
    {
        await _albumService.AddToFavoriteAlbumAsync(albumId, isAdding);
        return true;
    }

    public async Task<bool> AddTrackToAlbumAsync(AddTrackToAlbumRequest addTrackToAlbumRequest)
    {
        await _albumService.AddTrackToAlbumAsync(addTrackToAlbumRequest);
        return true;
    }

    public async Task<bool> RemoveTrackFromAlbumAsync(RemoveTrackFromAlbumRequest removeTrackFromAlbumRequest)
    {
        await _albumService.RemoveTrackFromAlbumAsync(removeTrackFromAlbumRequest);
        return true;
    }

    public async Task<bool> DeleteAlbumAsync(string albumId)
    {
        await _albumService.DeleteAlbumAsync(albumId);
        return true;
    }
}