using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Transactions;
public interface ITransactionService
{
    IQueryable<PaymentTransaction> GetPaymentTransactions();
    IQueryable<PayoutTransaction> GetPayoutTransactions();
    IQueryable<RefundTransaction> GetRefundTransactions();
}
