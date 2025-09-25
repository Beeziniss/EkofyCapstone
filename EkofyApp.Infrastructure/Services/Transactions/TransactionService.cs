using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Transactions;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Transactions;
public sealed class TransactionService(IUnitOfWork unitOfWork) : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Transaction> GetTransactions()
    {
        return _unitOfWork.GetCollection<Transaction>().AsQueryable();
    }
}
