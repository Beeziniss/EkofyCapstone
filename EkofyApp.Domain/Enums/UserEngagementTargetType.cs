using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum UserEngagementTargetType
{
    [EnumMember(Value = "Artist")]
    Artist,
    [EnumMember(Value = "Listener")]
    Listener,
    [EnumMember(Value = "Track")]
    Track,
    [EnumMember(Value = "Playlist")]
    Playlist,
    [EnumMember(Value = "Album")]
    Album
}
