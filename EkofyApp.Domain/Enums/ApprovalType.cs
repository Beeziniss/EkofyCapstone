using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum ApprovalType
{
    [EnumMember(Value = "TrackUpload")]
    TrackUpload,
    [EnumMember(Value = "ArtistRegistration")]
    ArtistRegistration
}
