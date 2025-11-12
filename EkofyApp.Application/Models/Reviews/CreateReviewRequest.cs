namespace EkofyApp.Application.Models.Reviews;
public sealed record class CreateReviewRequest
{
    public string PackageOrderId { get; init; } = null!;
    public int Rating { get; init; }
    public string Content { get; init; } = null!;
}
