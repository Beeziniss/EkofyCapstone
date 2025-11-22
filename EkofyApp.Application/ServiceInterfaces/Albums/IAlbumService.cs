using EkofyApp.Application.Models.Albums;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Albums;

public interface IAlbumService
{
    Task AddToFavoriteAlbumAsync(string albumId, bool isAdding);
    Task AddTrackToAlbumAsync(AddTrackToAlbumRequest addTrackToAlbumRequest);
    Task<bool> CheckAlbumInFavoriteAsync(string albumId);
    Task CreateAlbumAsync(CreateAlbumRequest createAlbumRequest);
    Task DeleteAlbumAsync(string albumId);
    IQueryable<Album> GetFavoriteAlbums();
    IQueryable<Album> GetAlbums();
    Task RemoveTrackFromAlbumAsync(RemoveTrackFromAlbumRequest removeTrackFromAlbumRequest);
    IQueryable<Album> SearchAlbums(string name);
}