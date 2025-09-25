using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Invoice))]
public sealed class InvoiceResolver
{
    public async Task<User?> GetUserAsync(
        [Parent] Invoice invoice,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await userByIdDataLoader.LoadAsync(invoice.UserId, cancellationToken);
    }

    public async Task<Transaction?> GetTransactionAsync(
        [Parent] Invoice invoice,
        DataLoaderCustomOneToOne<Transaction> transactionByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await transactionByIdDataLoader.LoadAsync(invoice.TransactionId, cancellationToken);
    }
}
