using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum PackageOrderStatus
{
    [EnumMember(Value = "Cancelled")]
    Cancelled,      // Đã hủy
    [EnumMember(Value = "Refunded")]
    Refunded,       // Đã hoàn tiền
    [EnumMember(Value = "InProgress")]
    InProgress,     // Đang thực hiện
    [EnumMember(Value = "Completed")]
    Completed,      // Đã hoàn thành
}
