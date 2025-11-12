namespace EkofyApp.Application.Models.Reviews;
public sealed record class UpdateReviewRequest
{
    public string PackageOrderId { get; init; } = null!;
    public int? Rating { get; init; }
    public string? Comment { get; init; }
}
