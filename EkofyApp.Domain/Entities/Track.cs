using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Track : Auditable, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the track

    public string Name { get; set; } = null!; // Name of the track, e.g., "Song Title"
    public string? Description { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> CategoryIds { get; set; } = []; // List of category IDs this track belongs to
    public List<string> Tags { get; set; } = []; // e.g., "music", "podcast", etc.

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> MainArtistIds { get; set; } = [];
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> FeaturedArtistIds { get; set; } = [];

    public AudioFeature AudioFeature { get; set; } = null!; // Contains audio features
    public AudioFingerprint AudioFingerprint { get; set; } = null!; // Unique identifier for the audio content

    public bool IsApproved { get; set; } = false; // Indicates if the track is visible to users

    public long StreamCount { get; set; } = default; // Number of times the track has been streamed
    public long FavoriteCount { get; set; } = default; // Number of times the track has been favorited

    public string CoverImage { get; set; } = null!; // URL to the cover image of the track
    public string? PreviewVideo { get; set; } // URL to a preview video of the track, if available

    public bool IsExplicit { get; set; } // Indicates if the track contains explicit content
    public string? Lyrics { get; set; } // Full lyrics of the track, if available
    public List<SyncedLine> SyncedLyrics { get; set; } = []; // List of synced lyrics lines with timestamps

    public ReleaseInfo ReleaseInfo { get; set; } = null!; // Information about the track's release, including date and reason

    // TODO: Nên thêm thông tin về bản quyền, hợp đồng, v.v.
    // TODO: Có nên thêm verified track bởi moderator nào không
}
