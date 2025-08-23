using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Artist;
public enum ArtistType
{
    [EnumMember(Value = "Individual")]
    Individual,
    [EnumMember(Value = "Group")] // Thường dùng cho các nhóm nhạc nhỏ hoặc ca sĩ hát chung
    Group,
    [EnumMember(Value = "Band")] // Thường chỉ dùng cho các ban nhạc với nhạc cụ
    Band,
}
