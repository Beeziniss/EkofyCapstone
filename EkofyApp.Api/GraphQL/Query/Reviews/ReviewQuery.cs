using EkofyApp.Application.ServiceInterfaces.Reviews;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Reviews;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ReviewQuery(IReviewService reviewService)
{
    private readonly IReviewService _reviewService = reviewService;

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Review>]
    public IQueryable<Review> GetReviews()
    {
        return _reviewService.GetReviews();
    }
}
