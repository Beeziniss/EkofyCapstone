namespace EkofyApp.Application.Models.Stripes;
public sealed record class PriceResponse
{
    public string Id { get; init; } = null!;
    public string ProductId { get; init; } = null!;

    public string LookupKey { get; init; } = null!;
    public string Currency { get; init; } = null!;

    public long UnitAmount { get; init; }

    public string Interval { get; init; } = null!;
    public long IntervalCount { get; init; }
}
