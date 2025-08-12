using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Subcriptions;
public enum SubscriptionStatus
{
    [EnumMember(Value = "Inactive")]
    Inactive,
    [EnumMember(Value = "Active")]
    Active,
    [EnumMember(Value = "Deprecated")]
    Deprecated
}
