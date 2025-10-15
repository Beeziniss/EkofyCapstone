using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Reports;

public enum ReportType
{
    [EnumMember(Value = "Spam")]
    Spam,                   // Spam, qu?ng cáo
    
    [EnumMember(Value = "Harassment")]
    Harassment,             // Qu?y r?i, b?t n?t
    
    [EnumMember(Value = "HateSpeech")]
    HateSpeech,             // Phát ngôn thù ??ch
    
    [EnumMember(Value = "InappropriateContent")]
    InappropriateContent,   // N?i dung không phù h?p
    
    [EnumMember(Value = "Impersonation")]
    Impersonation,          // Gi? m?o danh tính
    
    [EnumMember(Value = "CopyrightViolation")]
    CopyrightViolation,     // Vi ph?m b?n quy?n
    
    [EnumMember(Value = "FakeAccount")]
    FakeAccount,            // Tài kho?n gi? m?o
    
    [EnumMember(Value = "ScamOrFraud")]
    ScamOrFraud,            // L?a ??o
    
    [EnumMember(Value = "SelfHarmOrDangerousContent")]
    SelfHarmOrDangerousContent,  // T? gây h?i ho?c n?i dung nguy hi?m
    
    [EnumMember(Value = "Other")]
    Other                   // Lý do khác
}
