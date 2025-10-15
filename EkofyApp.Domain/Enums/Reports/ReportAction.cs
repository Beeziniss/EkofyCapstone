using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Reports;

public enum ReportAction
{
    [EnumMember(Value = "NoAction")]
    NoAction,               // Không hành ??ng
    
    [EnumMember(Value = "Warning")]
    Warning,                // C?nh báo user
    
    [EnumMember(Value = "ContentRemoval")]
    ContentRemoval,         // Xóa n?i dung vi ph?m
    
    [EnumMember(Value = "Suspended")]
    Suspended,    // ?ình ch? t?m th?i (n days)
    
    [EnumMember(Value = "PermanentBan")]
    PermanentBan,           // C?m v?nh vi?n
    
    [EnumMember(Value = "EntitlementRestriction")]
    EntitlementRestriction      // H?n ch? tính n?ng tài kho?n
}
