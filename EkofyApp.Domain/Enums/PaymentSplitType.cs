using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum PaymentSplitType
{
    [EnumMember(Value = "advance_payment")]
    AdvancePayment,
    [EnumMember(Value = "completion_payment")]
    CompletionPayment,
    [EnumMember(Value = "platform_commission")]
    PlatformCommission
}