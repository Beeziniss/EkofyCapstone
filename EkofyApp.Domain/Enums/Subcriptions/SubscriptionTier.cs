using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Subcriptions;
public enum SubscriptionTier
{
    [EnumMember(Value = "Free")]
    Free,
    //[EnumMember(Value = "Basic")]
    //Basic,
    [EnumMember(Value = "Premium")]
    Premium,
    [EnumMember(Value = "Pro")]
    Pro
}

