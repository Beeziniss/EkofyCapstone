using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum SubscriptionType
{
    [EnumMember(Value = "Free")]
    Free,
    [EnumMember(Value = "Premium")]
    Premium,
    [EnumMember(Value = "Royal")]
    Royal
}

