using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Comment))]
public sealed class CommentResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] Comment comment, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == comment.CommenterId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Listener> GetListener([Parent] Comment comment, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Listener>().AsQueryable().Where(x => x.UserId == comment.CommenterId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetArtist([Parent] Comment comment, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => x.UserId == comment.CommenterId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Track> GetTrack([Parent] Comment comment, [Service] IUnitOfWork unitOfWork)
    {
        if(comment.CommentType != CommentType.Track)
        {
            return Enumerable.Empty<Track>().AsQueryable();
        }

        return unitOfWork.GetCollection<Track>().AsQueryable().Where(x => x.Id == comment.TargetId);
    }

    // TODO: Requests Hub comment resolver
}
