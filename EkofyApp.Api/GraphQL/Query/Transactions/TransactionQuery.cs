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
    [UseSorting<PaymentTransaction>]
    public IQueryable<PaymentTransaction> GetPaymentTransactions()
    {
        return _transactionService.GetPaymentTransactions();
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<PayoutTransaction>]
    public IQueryable<PayoutTransaction> GetPayoutTransactions()
    {
        return _transactionService.GetPayoutTransactions();
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<RefundTransaction>]
    public IQueryable<RefundTransaction> GetRefundTransactions()
    {
        return _transactionService.GetRefundTransactions();
    }
}
