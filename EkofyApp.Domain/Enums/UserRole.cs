using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum UserRole
{
    [EnumMember(Value = "Admin")]
    Admin,
    [EnumMember(Value = "Moderator")]
    Moderator,
    [EnumMember(Value = "Listener")]
    Listener,
    [EnumMember(Value = "Guest")]
    Guest
}
