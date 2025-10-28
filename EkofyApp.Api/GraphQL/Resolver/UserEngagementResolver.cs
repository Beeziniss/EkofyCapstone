using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(UserEngagement))]
public sealed class UserEngagementResolver
{
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetArtists([Parent] UserEngagement userEngagement, [Service] IUnitOfWork unitOfWork)
    {
        if (userEngagement.TargetType != UserEngagementTargetType.Artist)
        {
            return Enumerable.Empty<Artist>().AsQueryable();
        }

        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => x.UserId == userEngagement.TargetId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Listener> GetListeners([Parent] UserEngagement userEngagement, [Service] IUnitOfWork unitOfWork)
    {
        if (userEngagement.TargetType != UserEngagementTargetType.Listener)
        {
            return Enumerable.Empty<Listener>().AsQueryable();
        }

        return unitOfWork.GetCollection<Listener>().AsQueryable().Where(x => x.UserId == userEngagement.TargetId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTracks([Parent] UserEngagement userEngagement, [Service] IUnitOfWork unitOfWork)
    {
        if (userEngagement.TargetType != UserEngagementTargetType.Track)
        {
            return Enumerable.Empty<Track>().AsQueryable();
        }

        return unitOfWork.GetCollection<Track>().AsQueryable().Where(x => x.Id == userEngagement.TargetId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Playlist> GetPlaylists([Parent] UserEngagement userEngagement, [Service] IUnitOfWork unitOfWork)
    {
        if (userEngagement.TargetType != UserEngagementTargetType.Playlist)
        {
            return Enumerable.Empty<Playlist>().AsQueryable();
        }

        return unitOfWork.GetCollection<Playlist>().AsQueryable().Where(x => x.Id == userEngagement.TargetId);
    }
}
