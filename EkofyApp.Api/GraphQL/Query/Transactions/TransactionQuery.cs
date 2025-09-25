using EkofyApp.Application.ServiceInterfaces.Transactions;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Transactions;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class TransactionQuery(ITransactionService transactionService)
{
    private readonly ITransactionService _transactionService = transactionService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Transaction>]
    public IQueryable<Transaction> GetTransactions()
    {
        return _transactionService.GetTransactions();
    }
}
