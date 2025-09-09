using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum TransactionStatus
{
    [EnumMember(Value = "open")]
    Open,
    [EnumMember(Value = "completed")]
    Completed,
    [EnumMember(Value = "expired")]
    Expired,
}
