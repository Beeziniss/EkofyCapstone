using EkofyApp.Domain.Enums.Subcriptions;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreateSubScriptionPlanRequest
{
    public string LookupKey { get; init; } = null!;
    public string Name { get; init; } = null!;
    //public long UnitAmount { get; init; }
    public string Interval { get; init; } = null!; // "day", "week", "month", or "year"
    public long IntervalCount { get; init; }

    public List<string>? Images { get; set; } = null;
    public Dictionary<string, string>? Metadata { get; set; } = null;

    #region Subscription Details
    public SubscriptionTier SubscriptionTier { get; init; }
    public int SubscriptionVersion { get; init; }
    #endregion

    //public long TrialPeriodDays { get; init; }
}
