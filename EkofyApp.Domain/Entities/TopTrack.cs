using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;

public sealed class TopTrack : TimeStamped
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;
    public List<TopTrackInfo> TracksInfo { get; set; } = [];
}
