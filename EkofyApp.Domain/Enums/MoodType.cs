using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum MoodType
{
    [EnumMember(Value = "Happy")]
    Happy,
    [EnumMember(Value = "Calm")]
    Calm,
    [EnumMember(Value = "Sad")]
    Sad,
    [EnumMember(Value = "Angry")]
    Angry,
    [EnumMember(Value = "Relaxed")]
    Relaxed,
    [EnumMember(Value = "Energetic")]
    Energetic,
    [EnumMember(Value = "Dark")]
    Dark,
    [EnumMember(Value = "Romantic")]
    Romantic,
    [EnumMember(Value = "Chill")]
    Chill
}
