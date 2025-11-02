using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.Models.Stripes;
using Stripe;
using Account = Stripe.Account;
using PortalSession = Stripe.BillingPortal.Session;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
public interface IStripeService
{
    AccountLinkResponse CreateAccountOnboardingLink(string refreshUrl, string returnUrl);
    AccountLink CreateAccountOnboardingLinkTest(string refreshUrl, string returnUrl);
    Task<Customer> CreateCustomerAsync();
    Task CreateExpressConnectedAccount();
    Task<Account> CreateExpressConnectedAccountTest();
    Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreatePaymentCheckoutSessionRequest createPaymentCheckoutSessionRequest);

    //Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreateSubscriptionCheckoutSessionRequest createCheckoutSessionRequest);
    Task<CheckoutSessionResponse> CreateSubscriptionCheckoutSession(CreateSubscriptionCheckoutSessionRequest createCheckoutSessionRequest);
    Task<PaymentIntent> CreateTopupAsync(long amount, string currency = "usd");
    Task DeleteConnectedAccount(string accountId);
    Balance GetBalance();
    Task<bool> IsCustomerIdExisted();
    void TransferGroupArtist(string[] artistAccountIds, long amount, string groupId = "default");
    TransferResponse TransferToArtist(string artistAccountId, long amount, string description);
    
    // Payout methods
    Task<Payout> CreateStandardPayoutAsync(string connectedAccountId, long amount, string? description = null, Dictionary<string, string>? metadata = null, string currency = "sgd");
    Task<Payout> CreateInstantPayoutAsync(string connectedAccountId, long amount, string? description = null, string currency = "sgd");
    Task<Balance> GetConnectedAccountBalanceAsync(string connectedAccountId);
    Task CancelSubscriptionAtPeriodEndAsync();
    Task ResumeSubscriptionAsync();
    Task EscrowReleaseAsync(string packageOrderId);
}
