using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class PackageOrder
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string ClientId { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProviderId { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string ArtistPackageId { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string PaymentTransactionId { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = null!;

    //public PackageOrderStatus Status { get; set; }
    public string? Description { get; set; }
    public List<string> RequirementFiles { get; set; } = [];
    public int RevisionCount { get; set; }
    public List<PackageOrderDelivery> Deliveries { get; set; } = [];
    public DateTimeOffset Deadline { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public decimal PlatformFeePercentage { get; set; }
    public decimal ArtistFeePercentage { get; set; }
}
