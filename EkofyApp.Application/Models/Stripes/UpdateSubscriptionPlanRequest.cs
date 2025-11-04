namespace EkofyApp.Application.Models.Stripes;

public sealed class UpdateSubscriptionPlanRequest
{
    public string SubscriptionPlanId { get; init; } = null!;

    public List<CreatePriceRequest> NewPrices { get; init; } = [];

    public List<UpdatePriceRequest> UpdatePrices { get; init; } = [];

    public Dictionary<string, string>? Metadata { get; set; } = null;

    public List<string>? Images { get; set; } = null;

    public string? Name { get; set; } = null;
}