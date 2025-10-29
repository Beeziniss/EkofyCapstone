using EkofyApp.Application.ServiceInterfaces.Playlists;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Playlists;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class PlaylistQuery(IPlaylistService playlistService)
{
    private readonly IPlaylistService _playlistService = playlistService;

    //[AuthorizeRoles(HelperRoleBase.FullRoles)]
    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Playlist>]
    public IQueryable<Playlist> GetPlaylists()
    {
        return _playlistService.GetPlaylists();
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Playlist>]
    public IQueryable<Playlist> GetFavoritePlaylists()
    {
        return _playlistService.GetFavoritePlaylists();
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Playlist>]
    public IQueryable<Playlist> SearchPlaylists(string name)
    {
        return _playlistService.SearchPlaylists(name);
    }
}
