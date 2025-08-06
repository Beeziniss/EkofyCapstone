using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum RestrictionType
{
    [EnumMember(Value = "None")]
    None,
    [EnumMember(Value = "Banned")]
    Banned,
    [EnumMember(Value = "Suspended")]
    Suspended,
}
