namespace EkofyApp.Application.Models.Subscriptions;
public sealed record class UpdateEntitlementsSubscriptionRequest
{
    public string SubscriptionId { get; init; } = string.Empty;
    public List<UpdateEntitlementRequest> Entitlements { get; init; } = [];
}
