using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums.Coupons;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Coupon : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string StripeCouponId { get; set; } = null!; // coupon.Id từ Stripe

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Code { get; set; } = null!;

    public decimal PercentOff { get; set; }

    public CouponDurationType Duration { get; set; } // "once", "forever", "repeating (deprecated)"
    public CouponPurposeType Purpose { get; set; }

    public CouponStatus Status { get; set; }
}
