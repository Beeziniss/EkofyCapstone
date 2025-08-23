using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum AlbumType
{
    [EnumMember(Value = "Album")]
    Album, // A full-length album release
    [EnumMember(Value = "Single")]
    Single, // A single track release
    [EnumMember(Value = "EP")]
    EP, // An extended play release, typically shorter than an album but longer than a single
    [EnumMember(Value = "Compilation")]
    Compilation, // A collection of tracks from various artists or a specific artist
    [EnumMember(Value = "Remix")]
    Remix, // A reworked version of an existing track or album
    [EnumMember(Value = "Live")]
    Live, // A recording of a live performance
    [EnumMember(Value = "Soundtrack")]
    Soundtrack, // A collection of music from a film, TV show, or other media
}
