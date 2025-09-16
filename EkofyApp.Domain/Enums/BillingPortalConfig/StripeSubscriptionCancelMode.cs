using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.BillingPortalConfig;
public enum StripeSubscriptionCancelMode
{
    [EnumMember(Value = "immediately")]
    immediately,
    [EnumMember(Value = "at_period_end")]
    at_period_end,
}
