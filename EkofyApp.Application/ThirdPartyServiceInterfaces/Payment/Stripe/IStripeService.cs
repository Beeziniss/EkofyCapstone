using EkofyApp.Domain.Enums.Subcriptions;
using Stripe;
using Account = Stripe.Account;
using CheckoutSession = Stripe.Checkout.Session;
using PortalSession = Stripe.BillingPortal.Session;
using PortalSessionService = Stripe.BillingPortal.SessionService;
using StripeInvoice = Stripe.Invoice;
using StripeSubscription = Stripe.Subscription;
using Subscription = EkofyApp.Domain.Entities.Subscription;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
public interface IStripeService
{
    AccountLink CreateAccountOnboardingLink(string refreshUrl, string returnUrl);
    Task<Customer> CreateCustomerAsync(string? name);
    Task<PortalSession> CreateCustomerPortalSessionAsync(string returnUrl);
    Task<Account> CreateExpressConnectedAccount();
    Task<CheckoutSession> CreatePaymentCheckoutSessionAsync(SubscriptionTier subscriptionTier, int subscriptionVersion, string successUrl, string cancelUrl);
    Task<CheckoutSession> CreateSubscriptionCheckoutSession(SubscriptionTier subscriptionTier, int subscriptionVersion, string successUrl, string cancelUrl);
    Task<Price> CreateSubscriptionPlan(string lookupKey, string subscriptionPlanName, long unitAmount, long intervalCount = 1, List<string>? images = null, Dictionary<string, string>? metadata = null);
    Task<PaymentIntent> CreateTopupAsync(long amount, string currency = "usd");
    Task DeleteConnectedAccount(string accountId);
    Balance GetBalance();
}
