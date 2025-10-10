using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Subscriptions;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class SubscriptionQuery(ISubscriptionService subscriptionService)
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;

    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Subscription>]
    public IQueryable<Subscription> GetSubscriptions()
    {
        return _subscriptionService.GetSubscriptions();
    }
}
