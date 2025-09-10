using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Coupons;
public enum CouponDurationType
{
    [EnumMember(Value = "repeating")]
    repeating,
    [EnumMember(Value = "forever")]
    forever,
    [EnumMember(Value = "once")]
    once
}
