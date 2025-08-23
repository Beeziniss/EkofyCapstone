using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Artist;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class ArtistInfo : IEntityCustom
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the artist role
    public ArtistRole Role { get; set; }
}
