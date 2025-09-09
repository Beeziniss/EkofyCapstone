using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.BillingPortalConfig;
public enum BillingPortalConfigStatus
{
    [EnumMember(Value = "Inactive")]
    Inactive,
    [EnumMember(Value = "Active")]
    Active,
    [EnumMember(Value = "Deprecated")]
    Deprecated
}
