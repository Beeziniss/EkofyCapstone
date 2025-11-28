using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Reports;

public enum ReportType
{
    [EnumMember(Value = "Spam")]
    Spam,

    [EnumMember(Value = "Harassment")]
    Harassment,

    [EnumMember(Value = "HateSpeech")]
    HateSpeech,

    [EnumMember(Value = "InappropriateContent")]
    InappropriateContent,

    [EnumMember(Value = "Impersonation")]
    Impersonation,

    [EnumMember(Value = "CopyrightViolation")]
    CopyrightViolation,

    [EnumMember(Value = "FakeAccount")]
    FakeAccount,

    [EnumMember(Value = "ScamOrFraud")]
    ScamOrFraud,

    [EnumMember(Value = "SelfHarmOrDangerousContent")]
    SelfHarmOrDangerousContent,

    [EnumMember(Value = "PrivacyViolation")]
    PrivacyViolation,

    [EnumMember(Value = "UnapprovedUploadedTrack")]
    UnapprovedUploadedTrack,

    [EnumMember(Value = "Other")]
    Other // Lý do khác
}
