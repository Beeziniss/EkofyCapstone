using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum RecordingStatus
{
    [EnumMember(Value = "active")]
    Active,
    [EnumMember(Value = "inactive")]
    Inactive,
    [EnumMember(Value = "pending")]
    Pending
}
