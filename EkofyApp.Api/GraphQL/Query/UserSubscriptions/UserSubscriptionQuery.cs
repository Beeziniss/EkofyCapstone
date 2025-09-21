using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.UserSubscriptions;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class UserSubscriptionQuery(IUserSubscriptionService userSubscriptionService)
{
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<UserSubscription>]
    public IQueryable<UserSubscription> GetUserSubscriptions()
    {
        return _userSubscriptionService.GetUserSubscriptions();
    }
}
