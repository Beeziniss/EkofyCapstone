using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(PaymentTransaction))]
public sealed class TransactionResolver
{
    public async Task<User?> GetUserAsync(
        [Parent] PaymentTransaction transaction,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userByIdDataLoader);
        return await userByIdDataLoader.LoadAsync(transaction.UserId, cancellationToken);
    }
}
