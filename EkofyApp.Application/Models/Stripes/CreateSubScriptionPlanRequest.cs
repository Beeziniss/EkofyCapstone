namespace EkofyApp.Application.Models.Stripes;
public sealed class CreateSubScriptionPlanRequest
{
    public string LookupKey { get; init; } = null!;
    public string Name { get; init; } = null!;
    public long UnitAmount { get; init; }
    public long IntervalCount { get; init; }

    public List<string>? Images { get; set; } = null;
    public Dictionary<string, string>? Metadata { get; set; } = null;

    //public long TrialPeriodDays { get; init; }
}
