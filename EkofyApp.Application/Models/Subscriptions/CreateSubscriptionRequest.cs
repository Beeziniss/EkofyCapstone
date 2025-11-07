using EkofyApp.Domain.Enums.Subcriptions;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed record class CreateSubscriptionRequest
{
    public string Name { get; init; } = null!; // DisplayName of the subscription plan
    public string? Description { get; init; } // PackageDescription of the subscription plan
    public string Code { get; init; } = null!; // Unique code for the subscription plan

    public decimal Price { get; init; } // Amount of the subscription plan

    public SubscriptionTier Tier { get; init; } // Subscription tier (e.g., Free, Premium, etc.)
    public SubscriptionStatus Status { get; init; } // Status of the subscription
    //public List<CreateEntitlementRequest> Entitlements { get; init; } = []; // List of features included in the subscription plan
}
