using EkofyApp.Application.Models.Reviews;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.PackageOrders;
using EkofyApp.Domain.Entities;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(ArtistPackage))]
public class ArtistPackageResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Artist> GetArtist([Parent] ArtistPackage artistPackage, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => x.Id == artistPackage.ArtistId);
    }

    public async Task<ReviewResponse> GetReviewAsync([Parent] ArtistPackage artistPackage, [Service] IPackageOrderService packageOrderService)
    {
        return await packageOrderService.GetAverageRatingBaseOnPackageAsync(artistPackage.Id);
    }
}
