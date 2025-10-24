using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class PayoutTransaction : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Artist nhận tiền

    // TODO: Chưa biết có cần không
    // Resolved: Cần
    [BsonRepresentation(BsonType.ObjectId)]
    public string RoyaltyReportId { get; set; } = null!; // Liên kết tới report đã generate

    #region Stripe
    public string StripeTransferId { get; set; } = null!; // Transfer.EntitlementId từ Stripe
    public string StripePayoutId { get; set; } = null!; // Payout.Id từ Stripe
    public string DestinationAccountId { get; set; } = null!; // Stripe Connected Account
    #endregion

    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;

    public AggregationLevel Level { get; set; }
    public string Description { get; set; } = null!;
    
    // Payout specific fields
    public PayoutTransactionStatus Status { get; set; } // pending, paid, failed, canceled (from Stripe)
    public string? Method { get; set; } // standard, instant (from Stripe)
}
