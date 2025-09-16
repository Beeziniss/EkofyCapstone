namespace EkofyApp.Application.Models.Subscriptions;
public sealed record class CreateEffectiveEntitlementRequest
{
    public string SubscriptionId { get; set; } = null!; // Unique identifier for the subscription plan
    //public string SubscriptionCode { get; set; } = null!; // Code of the subscription plan, e.g., "premium", "pro", etc.
    //public int SubscriptionVersion { get; set; } // Version of the subscription plan, default is 1
    public List<string> FeatureCodes { get; set; } = [];
    public DateTimeOffset ValidUntil { get; set; }
}
