using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Subscriptions;
public interface ISubscriptionService
{
    Task CreateSubscriptionAsync(CreateSubscriptionRequest createSubscriptionRequest);
    Task CreateSubscriptionPlanAsync(CreateSubScriptionPlanRequest createSubScriptionPlanRequest);
    Task DeprecateSubscriptionAsync(string subscriptionId);

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
}
