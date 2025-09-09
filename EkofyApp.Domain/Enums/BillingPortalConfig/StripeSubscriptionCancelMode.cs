using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.BillingPortalConfig;
public enum StripeSubscriptionCancelMode
{
    [EnumMember(Value = "immediately")]
    Immediately,
    [EnumMember(Value = "at_period_end")]
    AtPeriodEnd,
}
