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
    public IQueryable<User> GetUser([Parent] RefundTransaction transaction, [Service] IUnitOfWork unitOfWork)
    {
        PaymentTransaction? paymentTransaction = unitOfWork.GetCollection<PaymentTransaction>()
            .Find(u => u.StripePaymentId == transaction.StripePaymentId)
            .Project<PaymentTransaction>(Builders<PaymentTransaction>.Projection
                .Include(x => x.Id)
                .Include(x => x.UserId))
            .FirstOrDefault();

        if (paymentTransaction == null)
        {
            return Enumerable.Empty<User>().AsQueryable();
        }

        return unitOfWork.GetCollection<User>()
            .AsQueryable()
            .Where(u => u.Id == paymentTransaction.UserId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Listener> GetListener([Parent] RefundTransaction transaction, [Service] IUnitOfWork unitOfWork)
    {
        PaymentTransaction? paymentTransaction = unitOfWork.GetCollection<PaymentTransaction>()
            .Find(u => u.StripePaymentId == transaction.StripePaymentId)
            .Project<PaymentTransaction>(Builders<PaymentTransaction>.Projection
                .Include(x => x.Id)
                .Include(x => x.UserId))
            .FirstOrDefault();

        if (paymentTransaction == null)
        {
            return Enumerable.Empty<Listener>().AsQueryable();
        }

        return unitOfWork.GetCollection<Listener>()
            .AsQueryable()
            .Where(l => l.UserId == paymentTransaction.UserId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetArtist([Parent] RefundTransaction transaction, [Service] IUnitOfWork unitOfWork)
    {
        PaymentTransaction? paymentTransaction = unitOfWork.GetCollection<PaymentTransaction>()
            .Find(u => u.StripePaymentId == transaction.StripePaymentId)
            .Project<PaymentTransaction>(Builders<PaymentTransaction>.Projection
                .Include(x => x.Id)
                .Include(x => x.UserId))
            .FirstOrDefault();

        if (paymentTransaction == null)
        {
            return Enumerable.Empty<Artist>().AsQueryable();
        }

        return unitOfWork.GetCollection<Artist>()
            .AsQueryable()
            .Where(a => a.UserId == paymentTransaction.UserId);
    }
}
