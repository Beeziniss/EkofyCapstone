using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Track : IEntityCustom
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
    public List<string> ArtistId { get; set; } = [];

    public AudioFeature AudioFeature { get; set; } // Contains audio features
    public AudioFingerprint AudioFingerprint { get; set; } // Unique identifier for the audio content

    public bool IsPublished { get; set; } = false; // Indicates if the track is public or private
    public bool IsApproved { get; set; } = false; // Indicates if the track is visible to users

    public long StreamCount { get; set; } = default; // Number of times the track has been streamed
    public long FavoriteCount { get; set; } = default; // Number of times the track has been favorited

    public string CoverImage { get; set; } = null!; // URL to the cover image of the track
    public string ThumbnailImage { get; set; } = null!; // URL to the thumbnail image of the track
    public string? PreviewVideo { get; set; } // URL to a preview video of the track, if available

    public string? Lyrics { get; set; } // Full lyrics of the track, if available
    public List<SyncedLine> SyncedLyrics { get; set; } = []; // List of synced lyrics lines with timestamps

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public DateTime? PublishedDate { get; set; }
}
