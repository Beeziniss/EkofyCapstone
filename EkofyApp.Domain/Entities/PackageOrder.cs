using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class PackageOrder : TimeStamped
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
    public string? PayoutTransactionId { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = null!;
    public string Requirements { get; set; } = null!;
    public PackageOrderStatus Status { get; set; }
    public int RevisionCount { get; set; }
    public List<PackageOrderDelivery> Deliveries { get; set; } = [];
    public int Duration { get; set; }
    public TimeSpan FreezedTime { get; set; } = TimeSpan.Zero;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? DisputedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool IsEscrowReleased { get; set; } = false;
    public decimal PlatformFeePercentage { get; set; }
    public decimal ArtistFeePercentage => 100m - PlatformFeePercentage;
    public Review? Review { get; set; }
    public string? ApprovedAutoJobId { get; set; }
    public string? OverdueJobId { get; set; }
}
