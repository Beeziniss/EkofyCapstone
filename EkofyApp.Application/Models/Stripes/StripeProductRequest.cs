namespace EkofyApp.Application.Models.Stripes;
public sealed record class StripeProductRequest
{
    public string Id { get; set; } = null!;
    public List<string> StripePriceIds { get; set; } = [];
}
