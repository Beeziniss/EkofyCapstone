using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(RefundTransaction))]
public sealed class RefundTransactionResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] RefundTransaction refund, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == refund.UserId);
    }

    [UseProjection]
    [UseFiltering] 
    [UseSorting]
    public IQueryable<PaymentTransaction> GetPaymentTransaction([Parent] RefundTransaction refund, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<PaymentTransaction>().AsQueryable().Where(p => p.Id == refund.PaymentTransactionId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetProcessedByUser([Parent] RefundTransaction refund, [Service] IUnitOfWork unitOfWork)
    {
        if (string.IsNullOrEmpty(refund.ProcessedByUserId))
            return unitOfWork.GetCollection<User>().AsQueryable().Where(u => false); // Return empty

        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == refund.ProcessedByUserId);
    }
}