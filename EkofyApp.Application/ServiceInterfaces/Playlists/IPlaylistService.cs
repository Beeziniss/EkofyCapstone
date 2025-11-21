using EkofyApp.Application.Models.Playlists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Playlists;
public interface IPlaylistService
{
    Task AddToFavoritePlaylistAsync(string playlistId, bool isAdding);
    Task AddToPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest);
    Task<bool> CheckPlaylistInFavoriteAsync(string playlistId);
    Task CreatePlaylistAsync(CreatePlaylistRequest createPlaylistRequest);
    Task DeletePlaylistAsync(string playlistId);
    IQueryable<Playlist> GetFavoritePlaylists();
    IQueryable<Playlist> GetOwnPlaylists();
    IQueryable<Playlist> GetPlaylists();
    Task RemoveFromPlaylistAsync(RemoveFromPlaylistRequest removeFromPlaylistRequest);
    IQueryable<Playlist> SearchPlaylists(string name);
    Task UpdatePlaylistAsync(UpdatePlaylistRequest updatePlaylistRequest);
    Task UpsertDailyPlaylistsFromRecommendationsAsync(Dictionary<string, IEnumerable<string>> recommendedTracks);
}
