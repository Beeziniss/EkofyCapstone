using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum MoodType
{
    [EnumMember(Value = "happy")]
    Happy,
    [EnumMember(Value = "calm")]
    Calm,
    [EnumMember(Value = "sad")]
    Sad,
    [EnumMember(Value = "angry")]
    Angry,
    [EnumMember(Value = "relaxed")]
    Relaxed,
    [EnumMember(Value = "energetic")]
    Energetic,
    [EnumMember(Value = "dark")]
    Dark,
    [EnumMember(Value = "romantic")]
    Romantic,
    [EnumMember(Value = "chill")]
    Chill
}
