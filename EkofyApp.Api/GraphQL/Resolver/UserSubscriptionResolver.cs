using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(UserSubscription))]
public sealed class UserSubscriptionResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] UserSubscription userSubscription, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == userSubscription.UserId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Subscription> GetSubscription([Parent] UserSubscription userSubscription, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Subscription>().AsQueryable().Where(u => u.Id == userSubscription.SubscriptionId);
    }
}
