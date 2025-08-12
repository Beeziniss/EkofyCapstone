using EkofyApp.Domain.Enums.Subcriptions;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed record class CreateSubscriptionRequest
{
    public string Name { get; init; } = null!; // Name of the subscription plan
    public string? Description { get; init; } // Description of the subscription plan
    public string Code { get; init; } = null!; // Unique code for the subscription plan
    public int Version { get; init; }

    public decimal Price { get; init; } // Price of the subscription plan

    public SubscriptionTier Tier { get; init; } // Subscription tier (e.g., Free, Premium, etc.)
    public List<CreateFeatureRequest> Features { get; init; } = []; // List of features included in the subscription plan
}
