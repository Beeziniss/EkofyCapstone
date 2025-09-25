using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Subscriptions;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class SubscriptionPlanQuery(ISubscriptionPlanService subscriptionPlanService)
{
    private readonly ISubscriptionPlanService _subscriptionPlanService = subscriptionPlanService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<SubscriptionPlan>]
    public IQueryable<SubscriptionPlan> GetSubscriptionPlans()
    {
        return _subscriptionPlanService.GetSubscriptionPlans();
    }
}
