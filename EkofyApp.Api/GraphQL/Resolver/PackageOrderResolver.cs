using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(PackageOrder))]
public sealed class PackageOrderResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ArtistPackage> GetPackage([Parent] PackageOrder packageOrder, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<ArtistPackage>().AsQueryable().Where(x => x.Id == packageOrder.ArtistPackageId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Listener> GetClient([Parent] PackageOrder packageOrder, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Listener>().AsQueryable().Where(x => x.UserId == packageOrder.ClientId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetProvider([Parent] PackageOrder packageOrder, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => x.UserId == packageOrder.ProviderId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<PaymentTransaction> GetPaymentTransaction([Parent] PackageOrder packageOrder, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<PaymentTransaction>().AsQueryable().Where(x => x.Id == packageOrder.PaymentTransactionId);
    }
}
