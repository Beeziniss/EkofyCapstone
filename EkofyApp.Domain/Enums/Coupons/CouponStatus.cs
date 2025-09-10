using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Coupons;
public enum CouponStatus
{
    [EnumMember(Value = "active")]
    Active,
    [EnumMember(Value = "inactive")]
    Inactive,
    [EnumMember(Value = "expired")]
    Expired,
    [EnumMember(Value = "deprecated")]
    Deprecated
}
