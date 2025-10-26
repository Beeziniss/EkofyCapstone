using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum EscrowTransactionStatus
{
    [EnumMember(Value = "pending")]
    Pending,
    [EnumMember(Value = "partial_released")]
    PartialReleased,
    [EnumMember(Value = "completed")]
    Completed,
    [EnumMember(Value = "disputed")]
    Disputed,
    [EnumMember(Value = "refunded")]
    Refunded,
    [EnumMember(Value = "cancelled")]
    Cancelled
}