namespace EkofyApp.Application.Models.Reviews;
public sealed record class ReviewResponse
{
    public int AverageRating { get; init; }
    public int TotalReviews { get; init; }
}
