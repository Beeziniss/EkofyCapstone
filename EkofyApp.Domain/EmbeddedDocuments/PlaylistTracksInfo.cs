using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class PlaylistTracksInfo
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!;
    public DateTime AddedTime { get; set; }
}
