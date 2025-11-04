using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class RefundTransaction : TimeStamped
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string StripePaymentId { get; set; } = null!;
    public decimal Amount { get; set; }
    public CurrencyType Currency { get; set; }
    public RefundReasonType Reason { get; set; }
    public RefundTransactionStatus Status { get; set; }
}
