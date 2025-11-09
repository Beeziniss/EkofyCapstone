using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Subscriptions;
public interface ISubscriptionService
{
    Task CreateSubscriptionAsync(CreateSubscriptionRequest createSubscriptionRequest);
    Task CreateSubscriptionPlanAsync(CreateSubScriptionPlanRequest createSubScriptionPlanRequest);
    Task UpdateSubscriptionPlanAsync(UpdateSubscriptionPlanRequest updateSubscriptionPlanRequest);
    Task UpdateMetadataSubscriptionAsync(UpdateMetdataSubscriptionRequest updateMetadataSubscriptionRequest);
    //Task DeleteEntitlementSubsriptionAsync(DeleteEntitlementsSubscriptionRequest deleteEntitlementsSubscriptionRequest);
    
    /// <summary>
    /// Activates a subscription version and deactivates all other versions of the same tier.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to activate.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ActivateSubscriptionAsync(string subscriptionId);

    /// <summary>
    /// Creates a new subscription for the user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="subscriptionCode">The code of the subscription.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean indicating success.</returns>
    //Task<bool> CreateSubscriptionAsync(string userId, string subscriptionCode);
    /// <summary>
    /// Cancels an existing subscription for the user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean indicating success.</returns>
    //Task<bool> CancelSubscriptionAsync(string userId);
    IQueryable<Subscription> GetSubscriptions();
    //Task UpdateEntitlementsSubscriptionAsync(UpdateEntitlementsSubscriptionRequest updateEntitlementsSubscriptionRequest);
}
