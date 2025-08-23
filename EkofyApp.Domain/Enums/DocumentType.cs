using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum DocumentType
{
    [EnumMember(Value = "License")]
    License,
    [EnumMember(Value = "Contract")]
    Contract,
    [EnumMember(Value = "Certificate")]
    Certificate,
    [EnumMember(Value = "Other")]
    Other
}
