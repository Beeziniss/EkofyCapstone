using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class EscrowCommissionPolicy : TimeStamped
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public CurrencyType Currency { get; set; } = CurrencyType.vnd;
    public decimal PlatformFeePercentage { get; set; }

    public long Version { get; set; }

    public PolicyStatus Status { get; set; } = PolicyStatus.Inactive;
}
