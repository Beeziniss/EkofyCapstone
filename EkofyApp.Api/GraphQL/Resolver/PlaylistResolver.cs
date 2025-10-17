using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Playlist))]
public sealed class PlaylistResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] Playlist playlist, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == playlist.UserId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTracks([Parent] Playlist playlist, [Service] IUnitOfWork unitOfWork)
    {
        IEnumerable<string> trackIds = playlist.TracksInfo.Select(t => t.TrackId).ToList();

        return unitOfWork.GetCollection<Track>().AsQueryable().Where(t => trackIds.Contains(t.Id));
    }
}
