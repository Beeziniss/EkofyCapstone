using EkofyApp.Application.Models.Reviews;
using EkofyApp.Application.ServiceInterfaces.Reviews;

namespace EkofyApp.Api.GraphQL.Mutation.Reviews;

[ExtendObjectType<MutationInitialization>]
[MutationType]
public sealed class ReviewMutation(IReviewService reviewService)
{
    private readonly IReviewService _reviewService = reviewService;

    public async Task<bool> CreateReviewAsync(CreateReviewRequest createReviewRequest)
    {
        await _reviewService.CreateReviewAsync(createReviewRequest);
        return true;
    }

    public async Task<bool> UpdateReviewAsync(UpdateReviewRequest updateReviewRequest)
    {
        await _reviewService.UpdateReviewAsync(updateReviewRequest);
        return true;
    }

    public async Task<bool> DeleteReviewSoftAsync(string reviewId)
    {
        await _reviewService.DeleteReviewSoftAsync(reviewId);
        return true;
    }

    public async Task<bool> DeleteReviewHardAsync(string reviewId)
    {
        await _reviewService.DeleteReviewHardAsync(reviewId);
        return true;
    }
}
