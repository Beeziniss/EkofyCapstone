using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Transaction))]
public sealed class TransactionResolver
{
    public async Task<User?> GetUserAsync(
        [Parent] Transaction transaction,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userByIdDataLoader);
        return await userByIdDataLoader.LoadAsync(transaction.UserId, cancellationToken);
    }
}
