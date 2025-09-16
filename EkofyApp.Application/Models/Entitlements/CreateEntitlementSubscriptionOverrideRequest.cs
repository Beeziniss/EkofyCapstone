namespace EkofyApp.Application.Models.Entitlements;
public sealed record class CreateEntitlementSubscriptionOverrideRequest
{
    public string SubscriptionCode { get; init; } = null!;
    public object Value { get; init; } = null!;
}
