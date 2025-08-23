using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Users;
public enum UserGender
{
    [EnumMember(Value = "Male")]
    Male,
    [EnumMember(Value = "Female")]
    Female,
    [EnumMember(Value = "Other")]
    Other,
    [EnumMember(Value = "NotSpecified")]
    NotSpecified
}
