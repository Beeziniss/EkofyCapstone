using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum PackageOrderStatus
{
    [EnumMember(Value = "Cancelled")]
    Cancelled,      // Đã hủy
    [EnumMember(Value = "Refund")]
    Refund,       // Đã hoàn tiền
    [EnumMember(Value = "InProgress")]
    InProgress,     // Đang thực hiện
    [EnumMember(Value = "Paid")]
    Paid,           // Đã thanh toán cho artist
    [EnumMember(Value = "Disputed")]
    Disputed,           // Đang tranh chấp
    [EnumMember(Value = "Completed")]
    Completed,           // trả tiền cho artist và đơn đã finish
}
