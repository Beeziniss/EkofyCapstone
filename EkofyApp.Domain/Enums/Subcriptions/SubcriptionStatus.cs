using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Subcriptions;
public enum SubcriptionStatus
{
    [EnumMember(Value = "Inactive")]
    Inactive,
    [EnumMember(Value = "Active")]
    Active,
    [EnumMember(Value = "Deprecated")]
    Deprecated
}
