using EkofyApp.Application.ServiceInterfaces.Playlists;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Playlists;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class PlaylistQuery(IPlaylistService playlistService)
{
    private readonly IPlaylistService _playlistService = playlistService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Playlist>]
    public IQueryable<Playlist> GetPlaylists()
    {
        return _playlistService.GetPlaylists();
    }
}
