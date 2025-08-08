using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class EntitlementOverride
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the entitlement override

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user

    public List<Feature> Features { get; set; } = []; // List of features that are overridden for the user
}
