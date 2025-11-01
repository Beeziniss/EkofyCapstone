using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum ReleaseStatus
{
    [EnumMember(Value = "Not Announced")]
    NotAnnounced,
    [EnumMember(Value = "Delayed")]
    Delayed,
    [EnumMember(Value = "Canceled")]
    Canceled,
    [EnumMember(Value = "Banned")]
    Banned,
    [EnumMember(Value = "Official")]
    Official
}
