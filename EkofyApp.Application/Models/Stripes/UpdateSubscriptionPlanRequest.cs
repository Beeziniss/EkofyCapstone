namespace EkofyApp.Application.Models.Stripes;

public sealed class UpdateSubscriptionPlanRequest
{
    /// <summary>
    /// The subscription plan ID to update
    /// </summary>
    public string SubscriptionPlanId { get; init; } = null!;

    /// <summary>
    /// New prices to add to the subscription plan (e.g., month, week, year)
    /// </summary>
    public List<CreatePriceRequest> NewPrices { get; init; } = [];

    /// <summary>
    /// Optional: Update product metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; } = null;

    /// <summary>
    /// Optional: Update product images
    /// </summary>
    public List<string>? Images { get; set; } = null;

    /// <summary>
    /// Optional: Update product name
    /// </summary>
    public string? Name { get; set; } = null;
}