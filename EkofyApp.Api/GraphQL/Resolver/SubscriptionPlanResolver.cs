using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(SubscriptionPlan))]
public sealed class SubscriptionPlanResolver
{
    public async Task<Subscription?> GetSubscriptionAsync(
        [Parent] SubscriptionPlan subscriptionPlan,
        DataLoaderCustomOneToOne<Subscription> subscriptionByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await subscriptionByIdDataLoader.LoadAsync(subscriptionPlan.SubscriptionId, cancellationToken);
    }
}
