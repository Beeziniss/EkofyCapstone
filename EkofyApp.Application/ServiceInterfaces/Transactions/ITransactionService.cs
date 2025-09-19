using System.Transactions;

namespace EkofyApp.Application.ServiceInterfaces.Transactions;
public interface ITransactionService
{
    IQueryable<Transaction> GetTransactions();
}
