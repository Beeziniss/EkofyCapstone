using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum Status
{
    [EnumMember(Value = "Active")]
    Active,
    [EnumMember(Value = "Inactive")]
    Inactive,
    [EnumMember(Value = "Banned")]
    Banned,
}
