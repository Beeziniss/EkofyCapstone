using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum AggregationLevel
{
    [EnumMember(Value = "none")]
    None,
    [EnumMember(Value = "recording")]
    Recording,
    [EnumMember(Value = "work")]
    Work,
    [EnumMember(Value = "full")]
    Full // Track + Recording + Work
}
