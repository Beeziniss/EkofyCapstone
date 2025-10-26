using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum RefundTransactionStatus
{
    [EnumMember(Value = "pending")]
    Pending,
    [EnumMember(Value = "succeeded")]
    Succeeded,
    [EnumMember(Value = "failed")]
    Failed,
    [EnumMember(Value = "canceled")]
    Canceled,
    [EnumMember(Value = "requires_action")]
    RequiresAction
}