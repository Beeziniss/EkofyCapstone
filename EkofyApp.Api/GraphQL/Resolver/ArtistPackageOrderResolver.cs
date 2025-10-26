using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(ArtistPackageOrder))]
public sealed class ArtistPackageOrderResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetBuyer([Parent] ArtistPackageOrder order, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == order.ClientId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetArtist([Parent] ArtistPackageOrder order, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(u => u.Id == order.ProviderId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ArtistPackage> GetArtistPackage([Parent] ArtistPackageOrder order, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<ArtistPackage>().AsQueryable().Where(ap => ap.Id == order.ArtistPackageId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<PaymentTransaction> GetPaymentTransaction([Parent] ArtistPackageOrder order, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<PaymentTransaction>().AsQueryable().Where(pt => pt.Id == order.PaymentTransactionId);
    }
}