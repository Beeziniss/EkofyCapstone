using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Stripes;
public sealed record class CreateSubscriptionCheckoutSessionRequest
{
    public string SubscriptionCode { get; init; } = null!;
    //public SubscriptionTier SubscriptionTier { get; init; }
    //public int SubscriptionVersion { get; init; }

    public PeriodTime Period { get; init; }

    public string SuccessUrl { get; init; } = null!;
    public string CancelUrl { get; init; } = null!;

    public bool IsSavePaymentMethod { get; init; } = false;

    //public List<string> CouponCodes { get; init; } = [];
}
