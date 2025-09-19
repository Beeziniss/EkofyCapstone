using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.UserSubscriptions;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class UserSubscriptionQuery(IUserSubscriptionService userSubscriptionService)
{
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;

    public IQueryable<UserSubscription> GetUserSubscriptions()
    {
        return _userSubscriptionService.GetUserSubscriptions();
    }
}
