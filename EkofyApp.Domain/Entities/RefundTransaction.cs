using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class RefundTransaction : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // User nhận refund

    [BsonRepresentation(BsonType.ObjectId)]
    public string PaymentTransactionId { get; set; } = null!; // PaymentTransaction gốc

    #region Stripe
    public string StripeRefundId { get; set; } = null!; // Refund.Id từ Stripe
    public string StripePaymentIntentId { get; set; } = null!; // PaymentIntent.Id được refund
    public string? StripeChargeId { get; set; } // Charge.Id (nếu có)
    #endregion

    public decimal Amount { get; set; } // Số tiền refund
    public string Currency { get; set; } = null!; // e.g., "usd", "vnd", "sgd"

    public RefundType Type { get; set; } // Full hoặc Partial
    public RefundTransactionStatus Status { get; set; } // pending, succeeded, failed, canceled
    
    public string? Reason { get; set; } // Lý do refund (tùy chọn)
    public string? Description { get; set; } // Mô tả thêm
    
    // Metadata từ Stripe
    public Dictionary<string, string>? Metadata { get; set; }
    
    // Admin info
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ProcessedByUserId { get; set; } // Admin xử lý refund
    public DateTimeOffset? ProcessedAt { get; set; }
}