namespace EkofyApp.Application.Models.Subscriptions;
public sealed record class UpdateEntitlementsSubscriptionRequest
{
    public string SubscriptionId { get; init; } = string.Empty;

    /// <summary>
    /// List of entitlements to add or update
    /// </summary>
    public List<UpdateEntitlementRequest> EntitlementsToAddOrUpdate { get; init; } = [];

    /// <summary>
    /// List of entitlement codes to remove
    /// </summary>
    public List<string> EntitlementCodesToRemove { get; init; } = [];
}
