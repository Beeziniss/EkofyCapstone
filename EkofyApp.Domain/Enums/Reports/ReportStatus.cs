using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Reports;

public enum ReportStatus
{
    [EnumMember(Value = "Pending")]
    Pending,            // Báo cáo mới, chờ xử lý
    
    [EnumMember(Value = "UnderReview")]
    UnderReview,        // Đang được moderator xem xét
    
    [EnumMember(Value = "Approved")]
    Approved,           // Báo cáo h?p l?, ?ã x? lý
    
    [EnumMember(Value = "Rejected")]
    Rejected,           // Báo cáo không h?p l?
    
    [EnumMember(Value = "Restored")]
    Restored,          // Báo cáo b? b? qua (không vi ph?m)
    
    [EnumMember(Value = "Escalated")]
    Escalated           // Báo cáo nghiêm tr?ng, chuy?n lên admin
}
