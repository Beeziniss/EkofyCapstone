using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Subscriptions;
public interface ISubscriptionService
{
    Task CreateSubscriptionAsync(CreateSubscriptionRequest createSubscriptionRequest);
    Task CreateSubscriptionPlanAsync(CreateSubScriptionPlanRequest createSubScriptionPlanRequest);
    //Task DeleteEntitlementSubsriptionAsync(DeleteEntitlementsSubscriptionRequest deleteEntitlementsSubscriptionRequest);
    Task DeprecateSubscriptionAsync(string subscriptionId);

    /// <summary>
    /// Tạo đăng ký mới cho user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="subscriptionCode">The code of the subscription.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean indicating success.</returns>
    //Task<bool> CreateSubscriptionAsync(string userId, string subscriptionCode);
    /// <summary>
    /// Hủy đăng ký hiện tại cho user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean indicating success.</returns>
    //Task<bool> CancelSubscriptionAsync(string userId);
    IQueryable<Subscription> GetSubscriptions();
    //Task UpdateEntitlementsSubscriptionAsync(UpdateEntitlementsSubscriptionRequest updateEntitlementsSubscriptionRequest);
}
