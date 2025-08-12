using EkofyApp.Application.Models.UserSubscriptions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
public interface IUserSubscriptionService
{
    Task CreateUserSubscriptionAsync(CreateUserSubscriptionRequest createUserSubscriptionRequest);
    IQueryable<UserSubscription> GetUserSubscriptions();
    Task UpdateStatusUserSubscriptionAsync(UpdateUserSubscriptionRequest updateUserSubscriptionRequest);
}
