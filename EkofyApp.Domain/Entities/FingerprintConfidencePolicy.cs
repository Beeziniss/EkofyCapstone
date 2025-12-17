using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;

public sealed class FingerprintConfidencePolicy
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public double RejectThreshold { get; set; }
    public double ManualReviewThreshold { get; set; }
}
