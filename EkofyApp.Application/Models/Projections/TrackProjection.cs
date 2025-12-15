using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Application.Models.Projections;

[BsonIgnoreExtraElements]
public sealed class TrackProjection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public Restriction Restriction { get; set; } = null!;
}
