using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum ApprovalType
{
    [EnumMember(Value = "TrackUpload")]
    TrackUpload,
    [EnumMember(Value = "WorkUpload")]
    WorkUpload,
    [EnumMember(Value = "RecordingUpload")]
    RecordingUpload,
    [EnumMember(Value = "ArtistRegistration")]
    ArtistRegistration,
    [EnumMember(Value = "DisputeResolution")]
    DisputeResolution
}
