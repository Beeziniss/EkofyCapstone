using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum WorkStatus
{
    [EnumMember(Value = "active")]
    Active,
    [EnumMember(Value = "inactive")]
    Inactive,
    [EnumMember(Value = "pending")]
    Pending
}
