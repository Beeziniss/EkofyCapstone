using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Reports;

public enum ReportPriority
{
    [EnumMember(Value = "Low")]
    Low,                    // ?u tiên th?p
    
    [EnumMember(Value = "Medium")]
    Medium,                 // ?u tiên trung bình
    
    [EnumMember(Value = "High")]
    High,                   // ?u tiên cao
    
    [EnumMember(Value = "Critical")]
    Critical                // Kh?n c?p
}
