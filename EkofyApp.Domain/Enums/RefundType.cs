using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum RefundType
{
    [EnumMember(Value = "full")]
    Full,
    [EnumMember(Value = "partial")]
    Partial
}