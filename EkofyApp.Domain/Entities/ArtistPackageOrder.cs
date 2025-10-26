using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class ArtistPackageOrder : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string ClientId { get; set; } = null!; // Listener/Artist/User mua

    [BsonRepresentation(BsonType.ObjectId)]
    public string ProviderId { get; set; } = null!; // Artist bán

    [BsonRepresentation(BsonType.ObjectId)]
    public string ArtistPackageId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string PaymentTransactionId { get; set; } = null!;

    public ArtistPackageOrderStatus Status { get; set; } = ArtistPackageOrderStatus.Pending;
    
    #region Order Details
    public string? OrderDescription { get; set; } = null!; // Yêu cầu của client
    public List<string> RequirementFiles { get; set; } = []; // Files client upload (brief, reference, etc)
    //public Dictionary<string, string> CustomRequirements { get; set; } = []; // Custom fields
    #endregion

    #region Delivery
    public List<string> DeliveryFiles { get; set; } = []; // Files provider giao
    public string? DeliveryNotes { get; set; }
    public DateTimeOffset? DeliveryAt { get; set; }
    public DateTimeOffset EstimatedDeliveryAt { get; set; }
    #endregion

    #region Revisions
    public int RevisionCount { get; set; } = 0;
    public int MaxRevisions { get; set; } = 3; // Default max revisions
    public List<OrderRevision> Revisions { get; set; } = [];
    #endregion

    #region Communication
    [BsonRepresentation(BsonType.ObjectId)]
    public string ConversationId { get; set; } = null!; // Conversation ID between client/provider về order này
    #endregion

    #region Completion & Review
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public int? ClientRating { get; set; } // 1-5 stars (client rate provider)
    public string? ClientReview { get; set; }
    public int? ProviderRating { get; set; } // Provider rate client
    public string? ProviderReview { get; set; }
    #endregion

    #region Payment & Escrow (Embedded Document)
    /// <summary>
    /// Escrow payment split configuration - only exists for escrow payments
    /// Null for regular one-time payments
    /// </summary>
    public PaymentSplit? EscrowPayment { get; set; }
    
    /// <summary>
    /// True if this order uses escrow payment system
    /// </summary>
    public bool IsEscrowPayment => EscrowPayment != null;
    #endregion
}

public sealed class OrderRevision
{
    public int RevisionNumber { get; set; }
    public string Feedback { get; set; } = null!;
    public List<string> FeedbackFiles { get; set; } = [];
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestedBy { get; set; } = null!; // Usually client
}