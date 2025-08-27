using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum TrackType
{
    [EnumMember(Value = "Original")]
    Original,
    [EnumMember(Value = "Cover")]
    Cover,
    [EnumMember(Value = "Remix")]
    Remix,
    [EnumMember(Value = "Live")]
    Live,
    [EnumMember(Value = "Sample")]
    Sample,
}
