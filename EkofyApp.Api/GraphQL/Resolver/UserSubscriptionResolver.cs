using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(UserSubscription))]
public sealed class UserSubscriptionResolver
{
    public async Task<User?> GetUserAsync(
        [Parent] UserSubscription userSubscription,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await userByIdDataLoader.LoadAsync(userSubscription.UserId, cancellationToken);
    }

    public async Task<Subscription?> GetSubscriptionAsync(
        [Parent] UserSubscription userSubscription,
        DataLoaderCustomOneToOne<Subscription> subscriptionByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await subscriptionByIdDataLoader.LoadAsync(userSubscription.SubscriptionId, cancellationToken);
    }
}
