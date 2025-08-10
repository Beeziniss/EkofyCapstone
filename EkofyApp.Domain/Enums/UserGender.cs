using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum UserGender
{
    [EnumMember(Value = "Male")]
    Male,
    [EnumMember(Value = "Female")]
    Female,
    [EnumMember(Value = "Other")]
    Other,
}
