using EkofyApp.Application.Models.Playlists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Playlists;
public interface IPlaylistService
{
    Task AddToFavoriteAsync(AddToPlaylistRequest addToPlaylistRequest);
    Task AddToPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest);
    Task DeletePlaylistAsync(string playlistId);
    IQueryable<Playlist> GetPlaylistsQueryable();
    Task RemoveFromPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest);
}
