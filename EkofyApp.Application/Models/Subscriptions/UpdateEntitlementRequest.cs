using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed record class UpdateEntitlementRequest
{
    public string? Name { get; init; } // DisplayName of the feature, e.g., "Advanced Analytics"
    public string Code { get; init; } = null!; // Unique code for the feature, e.g., "advanced_analytics"
    public string? Description { get; init; } // PackageDescription of the feature, e.g., "Access to advanced analytics tools"

    public EntitlementValueType? ValueType { get; init; } = null; // Type of the feature value, e.g., String, Number, Boolean
    public object? Value { get; init; } = null; // Value of the feature, can be a string, number, or boolean depending on the feature type
    public DateTimeOffset? ExpiredAt { get; init; } // Optional expiration date for the feature, if applicable
}
