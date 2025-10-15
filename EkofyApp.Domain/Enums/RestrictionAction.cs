using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum RestrictionAction
{
    [EnumMember(Value = "None")]
    None,
    [EnumMember(Value = "Report")]
    Report,
    [EnumMember(Value = "Comment")]
    Comment,
    [EnumMember(Value = "UploadTrack")]
    UploadTrack,
    [EnumMember(Value = "CreateRequestHub")]
    CreateRequestHub,
    [EnumMember(Value = "CreateDirectRequest")]
    CreateDirectRequest,
}
