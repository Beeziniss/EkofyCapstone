using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Users;
public enum UserRole
{
    [EnumMember(Value = "Admin")]
    Admin,
    [EnumMember(Value = "Moderator")]
    Moderator,
    [EnumMember(Value = "Artist")]
    Artist,
    [EnumMember(Value = "Listener")]
    Listener,
    [EnumMember(Value = "Guest")]
    Guest
}
