using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum UserEngagementAction
{
    [EnumMember(Value = "Follow")]
    Follow,
    [EnumMember(Value = "Like")]
    Like,
    [EnumMember(Value = "Bookmark")]
    Bookmark
}
