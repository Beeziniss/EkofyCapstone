using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Artist : IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string Name { get; set; }
    public string Genre { get; set; }
    public bool IsVerified { get; set; } = false; // Indicates if the artist is verified
    public bool IsBanned { get; set; } = false; // Indicates if the artist is banned
}
