using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Album : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the album
    public string Name { get; set; } = null!; // DisplayName of the album, e.g., "Album Title"
    public string? Description { get; set; } // Description of the album

    public AlbumType Type { get; set; } // Type of the album, e.g., Album, Single, EP, etc.

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> TrackIds { get; set; } = []; // List of track IDs in the album
    public List<ArtistInfo> ArtistInfos { get; set; } = []; // Information about the artists involved in the album

    public string CoverImage { get; set; } = null!; // URL to the cover image of the album
    public string? ThumbnailImage { get; set; } // URL to the thumbnail image of the album

    public ReleaseInfo ReleaseInfo { get; set; } = null!; // Information about the album's release, including date and reason
}
