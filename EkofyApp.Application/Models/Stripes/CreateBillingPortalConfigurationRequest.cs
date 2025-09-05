using EkofyApp.Domain.Enums.BillingPortalConfig;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Stripes;
public sealed record CreateBillingPortalConfigurationRequest
{
    // Customer
    public bool CustomerUpdateEnabled { get; set; }
    public List<CustomerUpdate> AllowedCustomerUpdates { get; set; } = [];

    public UserRole UserRole { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; }
    public int Version { get; set; }

    // Payment Method
    public bool PaymentMethodUpdateEnabled { get; set; }

    // Invoice
    public bool InvoiceHistoryEnabled { get; set; }

    // Subscription Cancel
    public bool SubscriptionCancelEnabled { get; set; }
    public StripeSubscriptionCancelMode Mode { get; set; }

    // Subscription Update
    public bool SuscriptionUpdateEnabled { get; set; }
    public List<StripeSubscriptionUpdate> AllowedSubscriptionUpdates { get; set; } = [];
    public List<StripeProductRequest> Products { get; set; } = [];
}
