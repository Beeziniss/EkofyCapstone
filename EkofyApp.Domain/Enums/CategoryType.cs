using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum CategoryType
{
    [EnumMember(Value = "Genre")]
    Genre,
    [EnumMember(Value = "Mood")]
    Mood,
}
