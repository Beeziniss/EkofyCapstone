using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;

namespace EkofyApp.Application.Models.Stripes;
public sealed record class CreateCheckoutSessionRequest
{
    public SubscriptionTier SubscriptionTier { get; init; }
    public int SubscriptionVersion { get; init; }

    public PeriodTime Period { get; init; }

    public string SuccessUrl { get; init; } = null!;
    public string CancelUrl { get; init; } = null!;

    public bool IsReceiptEmail { get; init; } = false;
    public bool IsSavePaymentMethod { get; init; } = false;

    public List<string> CouponCodes { get; init; } = [];
}
