using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum AudioFormat
{
    [EnumMember(Value = ".mp3")]
    MP3,
    [EnumMember(Value = ".wav")]
    WAV
}
