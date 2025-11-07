using EkofyApp.Application.Models.Reviews;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Reviews;
public interface IReviewService
{
    Task CreateReviewAsync(CreateReviewRequest createReviewRequest);
    IQueryable<Review> GetReviews();
    Task DeleteReviewHardAsync(string reviewId);
    Task DeleteReviewSoftAsync(string reviewId);
    Task UpdateReviewAsync(UpdateReviewRequest updateReviewRequest);
    Task<ReviewResponse> GetAverageRatingBaseOnPackageAsync(string packageId);
    Task<bool> CheckClientReviewedPackageOrderAsync(string clientId, string packageOrderId);
}
