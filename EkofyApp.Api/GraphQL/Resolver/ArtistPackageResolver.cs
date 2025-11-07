using EkofyApp.Application.Models.Reviews;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Reviews;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(ArtistPackage))]
public class ArtistPackageResolver
{
    public IQueryable<Artist> GetArtist([Parent] ArtistPackage artistPackage, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Artist>().AsQueryable().Where(x => x.Id == artistPackage.ArtistId);
    }

    public async Task<ReviewResponse> GetReviewAsync([Parent] ArtistPackage artistPackage, [Service] IReviewService reviewService)
    {
        return await reviewService.GetAverageRatingBaseOnPackageAsync(artistPackage.Id);
    }
}
