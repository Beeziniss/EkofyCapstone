using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Playlist : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the playlist
    [BsonRepresentation(BsonType.ObjectId)]
    public string ListenerId { get; set; } = null!; // ID of the listener who created the playlist

    public string Name { get; set; } = null!; // Name of the playlist, e.g., "Chill Vibes"
    public string? Description { get; set; } // Description of the playlist
    public string? CoverImage { get; set; } // URL to the cover image of the playlist

    public List<PlaylistTracksInfo> TracksInfo { get; set; } = [];

    public bool IsPublic { get; set; } = false; // Indicates if the playlist is public or private
}
