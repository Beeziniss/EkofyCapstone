using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Transactions;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Transactions;
public sealed class TransactionService(IUnitOfWork unitOfWork) : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<PaymentTransaction> SearchPaymentTransactions(string? searchTerm = null)
    {
        IQueryable<PaymentTransaction> query = _unitOfWork.GetCollection<PaymentTransaction>().AsQueryable();

        if (string.IsNullOrEmpty(searchTerm))
        {
            return query;
        }

        List<string> userIds = [];

        // Search by full name (unsigned)
        string unsignedSearchTerm = HelperMethod.ToUnsigned(searchTerm);
        IEnumerable<User> usersByName = _unitOfWork.GetCollection<User>()
            .Find(u => u.FullName != null && HelperMethod.ToUnsigned(u.FullName).Contains(unsignedSearchTerm))
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id))
            .ToEnumerable();

        userIds.AddRange(usersByName.Select(u => u.Id));

        // Search by email
        IEnumerable<User> usersByEmail = _unitOfWork.GetCollection<User>()
            .Find(u => u.Email != null && u.Email.Contains(searchTerm))
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id))
            .ToEnumerable();

        userIds.AddRange(usersByEmail.Select(u => u.Id));

        // Remove duplicates
        userIds = userIds.Distinct().ToList();

        if (userIds.Count != 0)
        {
            query = query.Where(t => userIds.Contains(t.UserId));
        }

        return query;
    }

    public IQueryable<PayoutTransaction> SearchPayTransactions(string? searchTerm = null)
    {
        IQueryable<PayoutTransaction> query = _unitOfWork.GetCollection<PayoutTransaction>().AsQueryable();
        if (string.IsNullOrEmpty(searchTerm))
        {
            return query;
        }

        List<string> userIds = [];

        // Search by full name (unsigned)
        string unsignedSearchTerm = HelperMethod.ToUnsigned(searchTerm);
        IEnumerable<User> usersByName = _unitOfWork.GetCollection<User>()
            .Find(u => u.FullName != null && HelperMethod.ToUnsigned(u.FullName).Contains(unsignedSearchTerm))
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id))
            .ToEnumerable();
        userIds.AddRange(usersByName.Select(u => u.Id));

        // Search by email
        IEnumerable<User> usersByEmail = _unitOfWork.GetCollection<User>()
            .Find(u => u.Email != null && u.Email.Contains(searchTerm))
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id))
            .ToEnumerable();
        userIds.AddRange(usersByEmail.Select(u => u.Id));

        // Remove duplicates
        userIds = userIds.Distinct().ToList();
        if (userIds.Count != 0)
        {
            query = query.Where(t => userIds.Contains(t.UserId));
        }
        return query;
    }

    public IQueryable<RefundTransaction> SearchRefundTransactions(string? searchTerm = null)
    {
        IQueryable<RefundTransaction> query = _unitOfWork.GetCollection<RefundTransaction>().AsQueryable();
        if (string.IsNullOrEmpty(searchTerm))
        {
            return query;
        }

        List<string> userIds = [];

        // Search by full name (unsigned)
        string unsignedSearchTerm = HelperMethod.ToUnsigned(searchTerm);
        IEnumerable<User> usersByName = _unitOfWork.GetCollection<User>()
            .Find(u => u.FullName != null && HelperMethod.ToUnsigned(u.FullName).Contains(unsignedSearchTerm))
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id))
            .ToEnumerable();
        userIds.AddRange(usersByName.Select(u => u.Id));

        // Search by email
        IEnumerable<User> usersByEmail = _unitOfWork.GetCollection<User>()
            .Find(u => u.Email != null && u.Email.Contains(searchTerm))
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id))
            .ToEnumerable();
        userIds.AddRange(usersByEmail.Select(u => u.Id));

        // Remove duplicates
        userIds = userIds.Distinct().ToList();
        
        if (userIds.Count != 0)
        {
            // Get PaymentTransaction IDs that belong to the found users
            List<string?> paymentTransactionIds = _unitOfWork.GetCollection<PaymentTransaction>()
                .Find(pt => userIds.Contains(pt.UserId))
                .Project<PaymentTransaction>(Builders<PaymentTransaction>.Projection
                    .Include(x => x.StripePaymentId))
                .ToEnumerable()
                .Where(pt => !string.IsNullOrEmpty(pt.StripePaymentId))
                .Select(pt => pt.StripePaymentId)
                .ToList();

            // Filter RefundTransactions by StripePaymentId
            query = query.Where(rt => paymentTransactionIds.Contains(rt.StripePaymentId));
        }

        return query;
    }

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
