using EkofyApp.Application.Models.Stripes;
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

    public async Task<bool> CreateSubscriptionPlanAsync(CreateSubScriptionPlanRequest createSubScriptionPlanRequest)
    {
        await _subscriptionService.CreateSubscriptionPlanAsync(createSubScriptionPlanRequest);
        return true;
    }

    public async Task<bool> DeprecateSubscriptionAsync(string subscriptionId)
    {
        await _subscriptionService.DeprecateSubscriptionAsync(subscriptionId);
        return true;
    }

    //public async Task<bool> UpdateEntitlementsSubscriptionAsync(UpdateEntitlementsSubscriptionRequest updateEntitlementsSubscriptionRequest)
    //{
    //    await _subscriptionService.UpdateEntitlementsSubscriptionAsync(updateEntitlementsSubscriptionRequest);
    //    return true;
    //}

    //public async Task<bool> DeleteEntitlementSubsriptionAsync(DeleteEntitlementsSubscriptionRequest deleteEntitlementsSubscriptionRequest)
    //{
    //    await _subscriptionService.DeleteEntitlementSubsriptionAsync(deleteEntitlementsSubscriptionRequest);
    //    return true;
    //}
}
