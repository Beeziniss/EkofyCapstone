using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum PayoutTransactionStatus
{
    [EnumMember(Value = "pending")]
    pending,
    [EnumMember(Value = "paid")]
    paid,
    [EnumMember(Value = "failed")]
    failed,
    [EnumMember(Value = "canceled")]
    canceled,
    [EnumMember(Value = "in_transit")]
    in_transit
}
