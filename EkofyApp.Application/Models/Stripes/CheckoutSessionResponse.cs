namespace EkofyApp.Application.Models.Stripes;
public sealed record class CheckoutSessionResponse
{
    public string Id { get; init; } = null!;
    public string Url { get; init; } = null!;

    public string SuccessUrl { get; init; } = null!;
    public string CancelUrl { get; init; } = null!;

    public string Status { get; init; } = null!;

    public DateTime Created { get; init; }
    public DateTime Expired { get; init; }

    public string Mode { get; init; } = null!;
}
