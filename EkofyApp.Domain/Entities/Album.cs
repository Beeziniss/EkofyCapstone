using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Album : TimeStamped
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the album
    public string Name { get; set; } = null!; // DisplayName of the album, e.g., "Album Title"
    public string NameUnsigned { get; set; } = null!; // Unsign version of the album name for search optimization
    public string? Description { get; set; } // PackageDescription of the album

    public AlbumType Type { get; set; } // Type of the album, e.g., Album, Single, EP, etc.

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> TrackIds { get; set; } = []; // List of track IDs in the album
    public List<ContributingArtist> ContributingArtists { get; set; } = []; // Information about the artists involved in the album

    public string CoverImage { get; set; } = null!; // URL to the cover image of the album
    public string? ThumbnailImage { get; set; } // URL to the thumbnail image of the album

    public ReleaseInfo ReleaseInfo { get; set; } = null!; // Information about the album's release, including date and reason

    public bool IsVisible { get; set; } = true; // Indicates if it is visible to users

    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = null!; // User ID of the album creator
}
