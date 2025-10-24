using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum PaymentTransactionStatus
{
    [EnumMember(Value = "pending")]
    Pending,
    [EnumMember(Value = "paid")]
    Paid,
    [EnumMember(Value = "unpaid")]
    Unpaid,
}
