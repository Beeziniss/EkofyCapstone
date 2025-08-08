using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Subcriptions;
public enum SubcriptionCycle
{
    [EnumMember(Value = "Weekly")]
    Weekly,
    [EnumMember(Value = "Monthly")]
    Monthly,
    [EnumMember(Value = "Yearly")]
    Yearly,
    [EnumMember(Value = "Lifetime")]
    Lifetime
}
