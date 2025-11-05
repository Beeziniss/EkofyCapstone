using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Stripes;

public sealed record class UpdatePriceRequest
{
    /// <summary>
    /// The Stripe Price ID to update
    /// </summary>
    public string StripePriceId { get; init; } = null!;

    /// <summary>
    /// New lookup key for the price (optional)
    /// </summary>
    public string? LookupKey { get; init; }

    /// <summary>
    /// Update the active status of the price
    /// </summary>
    public bool? Active { get; init; }

    /// <summary>
    /// Update the interval (day, week, month, year) - requires creating new price
    /// </summary>
    public PeriodTime? Interval { get; init; }

    /// <summary>
    /// Update the interval count - requires creating new price
    /// </summary>
    public long? IntervalCount { get; init; }

    /// <summary>
    /// Custom metadata for the price
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}