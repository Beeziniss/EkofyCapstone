namespace EkofyApp.Application.Models.UserSubscriptions;
public sealed record class CreateUserSubscriptionRequest
{
    public string SubscriptionId { get; init; } = null!; // Unique identifier for the subscription plan
    public DateTime PeriodStart { get; init; } // Start date of the subscription
    public DateTime PeriodEnd { get; init; } // Optional end date of the subscription, if applicable

    public bool AutoRenew { get; init; } = false; // Indicates if the subscription auto-renews

    // Provider linkage
    //public string? PaymentProvider { get; set; } // "Stripe", "Momo", ...
    //public string? ProviderCustomerId { get; set; }
    //public string? ProviderSubscriptionId { get; set; }
    //public string? LatestInvoiceId { get; set; }
}
