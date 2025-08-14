using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum EntitlementValueType
{
    [EnumMember(Value = "String")]
    String,
    [EnumMember(Value = "Int")]
    Int,
    [EnumMember(Value = "Decimal")]
    Decimal,
    [EnumMember(Value = "Long")]
    Long,
    [EnumMember(Value = "Double")]
    Double,
    [EnumMember(Value = "Boolean")]
    Boolean,
    [EnumMember(Value = "DateTime")]
    DateTime,
    [EnumMember(Value = "Object")]
    Object,
    [EnumMember(Value = "Array")]
    Array
}
