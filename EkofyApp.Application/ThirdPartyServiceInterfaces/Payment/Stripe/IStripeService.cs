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
    //Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreateCheckoutSessionRequest createCheckoutSessionRequest);
    Task<CheckoutSessionResponse> CreateSubscriptionCheckoutSession(CreateCheckoutSessionRequest createCheckoutSessionRequest);
    Task<PaymentIntent> CreateTopupAsync(long amount, string currency = "usd");
    Task DeleteConnectedAccount(string accountId);
    Balance GetBalance();
    Task<bool> IsCustomerIdExisted();
    void TransferGroupArtist(string[] artistAccountIds, long amount, string groupId = "default");
    TransferResponse TransferToArtist(string artistAccountId, long amount);
}
