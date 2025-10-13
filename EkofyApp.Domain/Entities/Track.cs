using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Track : Auditable, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the track

    public string Name { get; set; } = null!; // DisplayName of the track, e.g., "Song Title"
    public string NameUnsigned { get; set; } = null!; // Unsign version of the track name for search optimization
    public string? Description { get; set; }

    public TrackType Type { get; set; } // Original, Cover, Remix, Live, etc.

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> CategoryIds { get; set; } = []; // List of category IDs this track belongs to
    public List<string> Tags { get; set; } = []; // e.g., "music", "podcast", etc.

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> MainArtistIds { get; set; } = [];
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> FeaturedArtistIds { get; set; } = [];

    public AudioFeature AudioFeature { get; set; } = null!; // Contains audio features
    public string AlternativeDescription { get; set; } = null!;
    public AudioFingerprint? AudioFingerprint { get; set; } // Unique identifier for the audio content

    public long StreamCount { get; set; } = default; // Number of times the track has been streamed
    public long FavoriteCount { get; set; } = default; // Number of times the track has been favorited

    public string CoverImage { get; set; } = null!; // URL to the cover image of the track
    public string? PreviewVideo { get; set; } // URL to a preview video of the track, if available

    public bool IsExplicit { get; set; } // Indicates if the track contains explicit content
    public string? Lyrics { get; set; } // Full lyrics of the track, if available
    public List<SyncedLine> SyncedLyrics { get; set; } = []; // List of synced lyrics lines with timestamps

    // TODO: Cần xử lý field này vì nó chưa được sử dụng
    public bool IsMonetized { get; set; } // Indicates if the track is monetized

    public ReleaseInfo ReleaseInfo { get; set; } = null!; // Information about the track's release, including date and reason
    public Restriction Restriction { get; set; } = null!; // Information about any restrictions on the track

    // TODO: Nên thêm thông tin về bản quyền, hợp đồng, v.v.
    // TODO: Có nên thêm verified track bởi moderator nào không
    public float[] EmbeddingVector { get; set; } = null!; 
}
