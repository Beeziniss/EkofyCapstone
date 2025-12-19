using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;

public sealed class TopTrackInfo
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!;
    public string TrackName { get; set; } = null!;
    public string ArtistName { get; set; } = null!;
    public int PlayedCount { get; set; }
}
