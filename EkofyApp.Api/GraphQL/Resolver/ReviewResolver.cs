using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Reviews;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Review))]
public sealed class ReviewResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<PackageOrder> GetPackageOrder([Parent] Review review, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<PackageOrder>().AsQueryable().Where(x => x.Id == review.PackageOrderId);
    }

    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Listener> GetClient([Parent] Review review, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Listener>().AsQueryable().Where(x => x.UserId == review.ClientId);
    }

    public async Task<bool> CheckClientReviewedPackageOrderAsync([Parent] Review review, [Service] IReviewService reviewService)
    {
        return await reviewService.CheckClientReviewedPackageOrderAsync(review.ClientId, review.PackageOrderId);
    }
}
