using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum OneOffType
{
    [EnumMember(Value = "Payment")]
    Payment,
    [EnumMember(Value = "Refund")]
    Refund
}
