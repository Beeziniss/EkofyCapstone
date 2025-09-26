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

    public async Task<PaymentTransaction?> GetTransactionAsync(
        [Parent] Invoice invoice,
        DataLoaderCustomOneToOne<PaymentTransaction> transactionByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await transactionByIdDataLoader.LoadAsync(invoice.TransactionId, cancellationToken);
    }
}
