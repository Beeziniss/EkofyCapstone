using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Subcriptions;
public enum SubscriptionCycle
{
    [EnumMember(Value = "Weekly")]
    Weekly,
    [EnumMember(Value = "Monthly")]
    Monthly,
    [EnumMember(Value = "Yearly")]
    Yearly,
    [EnumMember(Value = "Lifetime")]
    Lifetime
}
