using EkofyApp.Domain.Enums.Subcriptions;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreateSubScriptionPlanRequest
{
    #region Price Details
    public List<CreatePriceRequest> Prices { get; init; } = [];
    #endregion

    #region Product Details
    public string Name { get; init; } = null!;
    public List<string>? Images { get; set; } = null;
    public Dictionary<string, string>? Metadata { get; set; } = null;
    #endregion

    #region Subscription Details
    public string SubscriptionCode { get; init; } = null!;
    //public SubscriptionTier SubscriptionTier { get; init; }
    //public int SubscriptionVersion { get; init; }
    #endregion

    //public long TrialPeriodDays { get; init; }
}
