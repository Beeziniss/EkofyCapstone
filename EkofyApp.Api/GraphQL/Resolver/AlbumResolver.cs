using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Albums;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Album))]
public sealed class AlbumResolver
{
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTracks([Parent] Album album, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Track>().AsQueryable().Where(t => album.TrackIds.Contains(t.Id));
    }

    [UseProjection]
    [UseFiltering]  
    [UseSorting]
    public IQueryable<Artist> GetArtists([Parent] Album album, [Service] IUnitOfWork unitOfWork)
    {
        IEnumerable<string> artistIds = album.ContributingArtists.Select(a => a.ArtistId).ToList();
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(a => artistIds.Contains(a.Id));
    }

    public async Task<bool> CheckAlbumInFavoriteAsync([Parent] Album album, [Service] IAlbumService albumService)
    {
        return await albumService.CheckAlbumInFavoriteAsync(album.Id);
    }
}