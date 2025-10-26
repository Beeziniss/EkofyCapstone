using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class PaymentSplit
{
    #region Payment Split Configuration
    public decimal TotalAmount { get; set; } // 100% amount from client
    public decimal AdvancePaymentAmount { get; set; } // 30% - Trả trước cho provider
    public decimal CompletionPaymentAmount { get; set; } // 60% - Trả sau khi hoàn thành
    public decimal PlatformCommissionAmount { get; set; } // 10% - Commission cho platform
    
    public decimal AdvancePaymentPercentage { get; set; } = 30m; // 30%
    public decimal CompletionPaymentPercentage { get; set; } = 60m; // 60%
    public decimal PlatformCommissionPercentage { get; set; } = 10m; // 10%
    #endregion

    #region Stripe Integration
    public string StripePaymentIntentId { get; set; } = null!;
    public string? StripeAdvanceTransferId { get; set; } // Transfer ID cho advance payment
    public string? StripeCompletionTransferId { get; set; } // Transfer ID cho completion payment
    public string ArtistStripeAccountId { get; set; } = null!; // Connected account của provider
    #endregion

    public string Currency { get; set; } = null!;
    public EscrowTransactionStatus Status { get; set; } = EscrowTransactionStatus.Pending;

    #region Timing
    public DateTimeOffset? AdvancePaymentReleasedAt { get; set; }
    public DateTimeOffset? CompletionPaymentReleasedAt { get; set; }
    public DateTimeOffset? OrderCompletedAt { get; set; } // Thời gian order hoàn thành
    public DateTimeOffset? AutoReleaseDate { get; set; } // Tự động release sau X ngày nếu không có dispute
    public DateTimeOffset CreatedAt { get; set; }
    #endregion

    #region Dispute Handling
    public bool IsDisputed { get; set; } = false;
    public string? DisputeReason { get; set; }
    public DateTimeOffset? DisputeCreatedAt { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public string? DisputeResolvedByUserId { get; set; } // Admin xử lý dispute
    #endregion

    public Dictionary<string, string>? Metadata { get; set; }
}