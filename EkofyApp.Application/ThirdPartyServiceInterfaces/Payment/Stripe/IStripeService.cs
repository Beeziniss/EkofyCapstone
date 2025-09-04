using EkofyApp.Application.Models.Stripes;
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
    AccountLinkResponse CreateAccountOnboardingLink(string refreshUrl, string returnUrl);
    AccountLink CreateAccountOnboardingLinkTest(string refreshUrl, string returnUrl);
    Task<Customer> CreateCustomerAsync();
    Task<PortalSession> CreateCustomerPortalSessionAsync(string returnUrl);
    Task CreateExpressConnectedAccount();
    Task<Account> CreateExpressConnectedAccountTest();
    Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreateCheckoutSessionRequest createCheckoutSessionRequest);
    Task<CheckoutSessionResponse> CreateSubscriptionCheckoutSession(CreateCheckoutSessionRequest createCheckoutSessionRequest);
    Task<PriceResponse> CreateSubscriptionPlanAsync(CreateSubScriptionPlanRequest createSubScriptionPlanRequest);
    Task<PaymentIntent> CreateTopupAsync(long amount, string currency = "usd");
    Task DeleteConnectedAccount(string accountId);
    Balance GetBalance();
    Task<bool> IsCustomerIdExisted();
    void TransferGroupArtist(string[] artistAccountIds, long amount, string groupId = "default");
    TransferResponse TransferToArtist(string artistAccountId, long amount);
}
