using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Artist;
public enum ArtistRole
{
    [EnumMember(Value = "Main")]
    Main, // The main artist of the release
    [EnumMember(Value = "Featured")]
    Featured, // An artist who is featured on the release but not the main artist
    [EnumMember(Value = "Remixer")]
    Remixer,
    [EnumMember(Value = "Composer")]
    Composer,
}
