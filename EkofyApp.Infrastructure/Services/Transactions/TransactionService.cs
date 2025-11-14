using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Transactions;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Transactions;
public sealed class TransactionService(IUnitOfWork unitOfWork) : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<PaymentTransaction> GetPaymentTransactions()
    {
        return _unitOfWork.GetCollection<PaymentTransaction>().AsQueryable();
    }

    public IQueryable<RefundTransaction> GetRefundTransactions()
    {
        return _unitOfWork.GetCollection<RefundTransaction>().AsQueryable();
    }

    public IQueryable<PayoutTransaction> GetPayoutTransactions()
    {
        return _unitOfWork.GetCollection<PayoutTransaction>().AsQueryable();
    }
}
