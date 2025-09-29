using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum MediaType
{
    [EnumMember(Value = "Audio")]
    Audio,
    [EnumMember(Value = "Video")]
    Video,
    [EnumMember(Value = "AudioAndVideo")]
    AudioAndVideo,
    [EnumMember(Value = "Any")]
    Any,
}
