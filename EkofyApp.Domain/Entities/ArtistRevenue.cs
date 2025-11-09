using EkofyApp.Domain.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class ArtistRevenue : TimeStamped
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;
    public decimal RoyaltyEarnings { get; set; }
    public decimal ServiceRevenue { get; set; } // Tiền chưa trừ hoa hồng
    public decimal GrossRevenue => RoyaltyEarnings + ServiceRevenue;
    public decimal RefundAmount { get; set; }
}
