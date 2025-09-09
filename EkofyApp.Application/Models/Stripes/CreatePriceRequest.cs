using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Stripes;
public sealed record class CreatePriceRequest
{
    public string LookupKey { get; init; } = null!;

    //public long UnitAmount { get; init; }
    public PeriodTime Interval { get; init; } // "day", "week", "month", or "year"
    public long IntervalCount { get; init; }
}
