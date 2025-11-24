using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;

public enum ConversationStatus
{
    [EnumMember(Value = "None")]
    None,           // Mặc định, chưa gửi request]
    [EnumMember(Value = "Pending")]
    Pending,        // Request gửi đi, đang chờ Artist/ người tạo request phản hồi
    [EnumMember(Value = "Confirmed")]
    Confirmed,     // Request đã được chấp nhận, chờ thanh toán
    [EnumMember(Value = "ConfirmedPayment")]
    ConfirmedPayment,
    [EnumMember(Value = "Cancelled")]
    Cancelled,      // Bị từ chối / hủy bỏ trước khi tạo order
    [EnumMember(Value = "InProgress")]
    InProgress,     // Đã tạo order, đang trao đổi trong quá trình thực hiện
    [EnumMember(Value = "Completed")]
    Completed       // Đơn hàng hoàn thành, kết thúc cuộc trò chuyện
}
