using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.Entities;
public sealed class PlatformRevenue
{
    public decimal TotalSubscriptionRevenue { get; set; }
    public decimal TotalComissionRevenue { get; set; }
    public decimal GrossRevenue => TotalSubscriptionRevenue + TotalComissionRevenue;
    public decimal TotalPayoutAmount { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public decimal NetRevenue => (TotalSubscriptionRevenue + TotalComissionRevenue) - TotalPayoutAmount - TotalRefundAmount;
    public CurrencyType Currency { get; set; } = CurrencyType.vnd;
}
