using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum UserStatus
{
    [EnumMember(Value = "Active")]
    Active,
    [EnumMember(Value = "Inactive")]
    Inactive,
    [EnumMember(Value = "Banned")]
    Banned,
}
