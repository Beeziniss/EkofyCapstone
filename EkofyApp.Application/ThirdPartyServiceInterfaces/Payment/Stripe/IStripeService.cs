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
    TransferResponse TransferToArtist(string artistAccountId, long amount);
    
    // Phương thức payout
    Task<Payout> CreatePayoutAsync(string connectedAccountId, long amount, string? description = null, string currency = "sgd");
    Task<Payout> CreateInstantPayoutAsync(string connectedAccountId, long amount, string? description = null, string currency = "sgd");
    Task<Balance> GetConnectedAccountBalanceAsync(string connectedAccountId);
    
    // Phương thức refund
    Task<RefundResponse> CreateRefundAsync(CreateRefundRequest request);
    Task<RefundResponse?> GetRefundAsync(string refundTransactionId);
    Task<List<RefundResponse>> ListRefundsAsync(string? paymentTransactionId = null, int limit = 10, string? startingAfter = null);
    
    // Phương thức escrow payment (split payment)
    Task<CheckoutSessionResponse> CreateEscrowPaymentCheckoutSessionAsync(CreateEscrowPaymentRequest request);
    Task<EscrowPaymentResponse> GetEscrowPaymentAsync(string escrowTransactionId);
    Task<List<EscrowPaymentResponse>> ListEscrowPaymentsAsync(string? userId = null, int limit = 10);
    Task<EscrowPaymentResponse> ReleaseAdvancePaymentAsync(string escrowTransactionId);
    Task<EscrowPaymentResponse> ReleaseCompletionPaymentAsync(string escrowTransactionId);
    Task<EscrowPaymentResponse> ConfirmOrderCompletionAsync(ConfirmOrderCompletionRequest request);
}
