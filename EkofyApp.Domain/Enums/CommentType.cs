using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum CommentType
{
    [EnumMember(Value = "Track")]
    Track,
    [EnumMember(Value = "Playlist")]
    Playlist,
    [EnumMember(Value = "Album")]
    Album,
    [EnumMember(Value = "Request")]
    Request
}
