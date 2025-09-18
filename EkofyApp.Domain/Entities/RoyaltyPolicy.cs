using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class RoyaltyPolicy : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public decimal RatePerStream { get; set; }
    public CurrencyType Currency { get; set; } = CurrencyType.vnd;
    public decimal RecordingPercentage { get; set; }
    public decimal WorkPercentage { get; set; }

    public long Version { get; set; }
    public bool IsActive { get; set; }
}
