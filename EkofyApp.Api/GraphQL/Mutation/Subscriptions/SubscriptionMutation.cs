using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;

namespace EkofyApp.Api.GraphQL.Mutation.Subscriptions;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class SubscriptionMutation(ISubscriptionService subscriptionService)
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;

    public async Task<bool> CreateSubscriptionAsync(CreateSubscriptionRequest createSubscriptionRequest)
    {
        await _subscriptionService.CreateSubscriptionAsync(createSubscriptionRequest);
        return true;
    }
}
