using EkofyApp.Domain.Base;
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
    [BsonRepresentation(BsonType.ObjectId)]
    public string RoyaltyReportId { get; set; } = null!; // Liên kết tới report đã generate

    #region Stripe
    public string StripeTransferId { get; set; } = null!; // Transfer.Id từ Stripe
    public string DestinationAccountId { get; set; } = null!; // Stripe Connected Account
    #endregion

    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;

    public string Description { get; set; } = null!;
    //public PayoutStatus Status { get; set; } // pending, succeeded, failed
}
