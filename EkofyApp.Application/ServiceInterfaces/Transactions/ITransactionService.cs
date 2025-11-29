using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Transactions;
public interface ITransactionService
{
    IQueryable<PaymentTransaction> GetPaymentTransactions();
    IQueryable<PayoutTransaction> GetPayoutTransactions();
    IQueryable<RefundTransaction> GetRefundTransactions();
    IQueryable<PaymentTransaction> SearchPaymentTransactions(string? searchTerm = null);
    IQueryable<PayoutTransaction> SearchPayoutTransactions(string? searchTerm = null);
    IQueryable<RefundTransaction> SearchRefundTransactions(string? searchTerm = null);
}
