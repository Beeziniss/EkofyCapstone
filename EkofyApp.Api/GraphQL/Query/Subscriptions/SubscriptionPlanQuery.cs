using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Subscriptions;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class SubscriptionPlanQuery(ISubscriptionPlanService subscriptionPlanService)
{
    private readonly ISubscriptionPlanService _subscriptionPlanService = subscriptionPlanService;

    public IQueryable<SubscriptionPlan> GetSubscriptionPlans()
    {
        return _subscriptionPlanService.GetSubscriptionPlans();
    }
}
