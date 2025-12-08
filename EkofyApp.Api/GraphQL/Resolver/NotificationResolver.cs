using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Notification))]
public sealed class NotificationResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetActor([Parent] Notification notification, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == notification.ActorId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetTarget([Parent] Notification notification, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == notification.TargetId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Listener> GetActorListener([Parent] Notification notification, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Listener>().AsQueryable().Where(x => x.UserId == notification.ActorId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Listener> GetTargetListener([Parent] Notification notification, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Listener>().AsQueryable().Where(x => x.UserId == notification.TargetId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetActorArtist([Parent] Notification notification, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => x.UserId == notification.ActorId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetTargetArtist([Parent] Notification notification, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => x.UserId == notification.TargetId);
    }
}
