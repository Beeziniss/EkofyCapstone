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
}
