using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
public interface IUserSubscriptionService
{
    IQueryable<UserSubscription> GetUserSubscriptions();
    Task CreateUserSubscriptionAsync(IClientSessionHandle? session, string userId, string subscriptionId, DateTimeOffset periodStart, DateTimeOffset? periodEnd = null);
    Task CreateUserSubscriptionAsync(IClientSessionHandle? session, string userId, string subscriptionId, string stripeSubscriptionId, DateTimeOffset periodStart, DateTimeOffset? periodEnd = null);
    Task UpdateStatusUserSubscriptionAsync(IClientSessionHandle? session, string userId, bool cancelAtEndOfPeriod, DateTimeOffset? canceledAt, bool isActive, bool isRenew);
    Task VerifyUserSubscriptionAsync();
}
