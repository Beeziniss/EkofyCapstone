using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class SubscriptionSnapshot
{
    #region Subscription
    public string SubscriptionName { get; set; } = null!;
    public string? SubscriptionDescription { get; set; }
    public string SubscriptionCode { get; set; } = null!; // Unique code for the subscription
    public int SubscriptionVersion { get; init; }

    public decimal SubscriptionAmount { get; set; }
    public CurrencyType SubscriptionCurrency { get; set; } = CurrencyType.vnd; // Default currency is vnd

    public SubscriptionTier SubscriptionTier { get; set; } // TODO: Cân nhắc có nên embed không
    public SubscriptionStatus SubscriptionStatus { get; set; }
    #endregion

    #region Subscription Plan
    public List<SubscriptionPlanPrice> SubscriptionPlanPrices { get; set; } = null!; // Snapshot of the prices at the time of transaction
    public string StripeProductId { get; set; } = null!;
    public bool StripeProductActive { get; set; }
    public string StripeProductName { get; set; } = null!;
    public List<string>? StripeProductImages { get; set; } = null;
    public string StripeProductType { get; set; } = null!; // "service" or "good"
    public List<Metadata>? StripeProductMetadata { get; set; } = null;
    #endregion
}
