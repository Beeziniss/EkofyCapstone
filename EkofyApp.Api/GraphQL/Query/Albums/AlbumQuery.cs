using EkofyApp.Application.ServiceInterfaces.Albums;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Albums;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class AlbumQuery(IAlbumService albumService)
{
    private readonly IAlbumService _albumService = albumService;

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Album>]
    public IQueryable<Album> GetAlbums()
    {
        return _albumService.GetAlbums();
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Album>]
    public IQueryable<Album> GetFavoriteAlbums()
    {
        return _albumService.GetFavoriteAlbums();
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Album>]
    public IQueryable<Album> SearchAlbums(string name)
    {
        return _albumService.SearchAlbums(name);
    }

    [AllowAnonymous]
    public async Task<bool> CheckAlbumInFavoriteAsync(string albumId)
    {
        return await _albumService.CheckAlbumInFavoriteAsync(albumId);
    }
}