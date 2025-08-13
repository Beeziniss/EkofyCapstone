using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Playlists;
public interface IPlaylistService
{
    IQueryable<Playlist> GetPlaylistsQueryable();
}
