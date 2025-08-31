using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum PeriodTime
{
    [EnumMember(Value = "day")]
    day,
    [EnumMember(Value = "week")]
    week,
    [EnumMember(Value = "month")]
    month,
    [EnumMember(Value = "year")]
    year
}
