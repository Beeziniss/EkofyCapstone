using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum RefundTransactionStatus
{
    [EnumMember(Value = "pending")]
    pending,
    [EnumMember(Value = "succeeded")]
    succeeded,
    [EnumMember(Value = "failed")]
    failed,
    [EnumMember(Value = "requires_action")]
    requires_action,
    [EnumMember(Value = "canceled")]
    canceled
}
