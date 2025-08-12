namespace EkofyApp.Application.Models.UserSubscriptions;
public sealed record class UpdateUserSubscriptionRequest
{
    public string UserId { get; init; } = null!; // Unique identifier for the user
    public string SubscriptionId { get; init; } = null!; // Unique identifier for the subscription plan

    public bool CancelAtEndOfPeriod { get; init; } = false; // Indicates if the subscription should cancel at the end of the period
    public DateTime? CanceledAt { get; init; } // Optional cancellation date, if applicable
}
