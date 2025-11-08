using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.Entities;
public sealed class PlatformRevenue : TimeStamped
{
    // Revenue streams
    public decimal SubscriptionRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal GrossRevenue => SubscriptionRevenue + ServiceRevenue;

    // Deductions
    public decimal RoyaltyPayoutAmount { get; set; }
    public decimal ServicePayoutAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal TotalPayoutAmount => RoyaltyPayoutAmount + ServicePayoutAmount;
    public decimal GrossDeductions => RoyaltyPayoutAmount + ServicePayoutAmount + RefundAmount;

    // Profits
    public decimal CommissionProfit => ServiceRevenue - ServicePayoutAmount;
    public decimal NetProfit => (SubscriptionRevenue + ServiceRevenue) - (RoyaltyPayoutAmount + ServicePayoutAmount + RefundAmount);

    public CurrencyType Currency { get; set; } = CurrencyType.vnd;
}
