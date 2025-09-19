using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Subscriptions;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class SubscriptionQuery(ISubscriptionService subscriptionService)
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;

    public IQueryable<Subscription> GetSubscriptions()
    {
        return _subscriptionService.GetSubscriptions();
    }
}
