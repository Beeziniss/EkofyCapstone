using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Coupons;
public enum CouponPurposeType
{
    [EnumMember(Value = "General")]
    General,         // Dùng cho mọi thứ
    [EnumMember(Value = "AutoService")]
    AutoService,     // Dùng cho dịch vụ tự động
    [EnumMember(Value = "AnnualPlanDiscount")]
    AnnualPlanDiscount, // Giảm giá cho gói thanh toán theo năm
    //ReferralBonus,   // Dùng cho chương trình giới thiệu
    //EventPromotion,  // Dùng cho sự kiện
    //Onboarding       // Dành cho người dùng mới
}
