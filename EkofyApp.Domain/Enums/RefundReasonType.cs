using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum RefundReasonType
{
    [EnumMember(Value = "duplicate")]
    duplicate,
    [EnumMember(Value = "fraudulent")]
    fraudulent,
    [EnumMember(Value = "requested_by_customer")]
    requested_by_customer,
}
