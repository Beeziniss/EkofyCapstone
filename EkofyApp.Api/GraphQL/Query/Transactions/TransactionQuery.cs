using EkofyApp.Application.ServiceInterfaces.Transactions;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Transactions;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class TransactionQuery(ITransactionService transactionService)
{
    private readonly ITransactionService _transactionService = transactionService;

    public IQueryable<Transaction> GetTransactions()
    {
        return _transactionService.GetTransactions();
    }
}
