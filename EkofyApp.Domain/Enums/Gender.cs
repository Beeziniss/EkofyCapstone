using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum Gender
{
    [EnumMember(Value = "Male")]
    Male,
    [EnumMember(Value = "Female")]
    Female,
    [EnumMember(Value = "Other")]
    Other,
}
