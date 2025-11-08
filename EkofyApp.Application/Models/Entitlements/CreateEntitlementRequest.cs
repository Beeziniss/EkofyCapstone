using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Entitlements;
public sealed record class CreateEntitlementRequest
{
    public string Name { get; init; } = null!; // Name of the entitlement
    public string Code { get; init; } = null!; // Unique code for the entitlement
    public string Description { get; init; } = null!; // PackageDescription of the entitlement
    public EntitlementValueType ValueType { get; set; } // Type of the feature value, e.g., String, Number, Boolean
    public List<CreateEntitlementRoleDefaultRequest> DefaultValues { get; init; } = []; // Default values based on user roles
    public List<CreateEntitlementSubscriptionOverrideRequest> SubscriptionOverrides { get; init; } = []; // Overrides based on subscription codes
    public bool IsActive { get; init; } // Indicates if the entitlement is active
}
