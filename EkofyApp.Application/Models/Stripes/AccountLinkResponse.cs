namespace EkofyApp.Application.Models.Stripes;
public sealed record class AccountLinkResponse
{
    public string AccountId { get; init; } = null!;
    public string Url { get; init; } = null!;
    public string RefreshUrl { get; init; } = null!;
    public string ReturnUrl { get; init; } = null!;
    public string Type { get; init; } = null!;

    public DateTime Created { get; init; }
    public DateTime Expired { get; init; }
}
