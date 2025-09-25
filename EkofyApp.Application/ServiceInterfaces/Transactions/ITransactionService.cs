using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Transactions;
public interface ITransactionService
{
    IQueryable<Transaction> GetTransactions();
}
