using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Reports;

public enum ReportStatus
{
    [EnumMember(Value = "Pending")]
    Pending,            // Báo cáo m?i, ch? x? lý
    
    [EnumMember(Value = "UnderReview")]
    UnderReview,        // ?ang ???c moderator xem xét
    
    [EnumMember(Value = "Approved")]
    Approved,           // Báo cáo h?p l?, ?ã x? lý
    
    [EnumMember(Value = "Rejected")]
    Rejected,           // Báo cáo không h?p l?
    
    [EnumMember(Value = "Dismissed")]
    Dismissed,          // Báo cáo b? b? qua (không vi ph?m)
    
    [EnumMember(Value = "Escalated")]
    Escalated           // Báo cáo nghiêm tr?ng, chuy?n lên admin
}
