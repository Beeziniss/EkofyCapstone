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
    //[EnumMember(Value = "Completed")]
    //Completed,      // Đã hoàn thành
    [EnumMember(Value = "Paid")]
    Paid,           // Đã thanh toán cho artist
    [EnumMember(Value = "Disputed")]
    Disputed,           // Đang tranh chấp
    [EnumMember(Value = "Dispersed")]
    Dispersed,           // trả tiền cho artist và đơn đã finish
    //[EnumMember(Value = "Pending")]
    //Pending
}
