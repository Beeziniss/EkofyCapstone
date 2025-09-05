using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.BillingPortalConfig;
public enum StripeSubscriptionUpdate
{
    [EnumMember(Value = "price")]
    Price,
    [EnumMember(Value = "promotion_code")]
    PromotionCode,
    [EnumMember(Value = "quantity")]
    Quantity
}
