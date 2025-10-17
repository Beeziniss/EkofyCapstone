using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(SubscriptionPlan))]
public sealed class SubscriptionPlanResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Subscription> GetSubscription([Parent] SubscriptionPlan subscriptionPlan, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Subscription>().AsQueryable().Where(s => s.Id == subscriptionPlan.SubscriptionId);
    }
}
