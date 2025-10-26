using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;

namespace EkofyApp.Api.GraphQL.Mutation.Payment.Stripes;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class StripeMutation(IStripeService stripeService)
{
    private readonly IStripeService _stripeService = stripeService;

    public async Task<AccountLinkResponse> CreateExpressConnectedAccountAsync(string refreshUrl = "https://ekofy.com/refresh", string returnUrl = "https://ekofy.com/return")
    {
        await _stripeService.CreateExpressConnectedAccount();

        AccountLinkResponse accountLink = _stripeService.CreateAccountOnboardingLink(
            refreshUrl: refreshUrl,
            returnUrl: returnUrl);

        return accountLink;
    }

    public async Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreatePaymentCheckoutSessionRequest createPaymentCheckoutSessionRequest)
    {
        if (!await _stripeService.IsCustomerIdExisted())
        {
            await _stripeService.CreateCustomerAsync();
        }

        return await _stripeService.CreatePaymentCheckoutSessionAsync(createPaymentCheckoutSessionRequest);
    }

    public async Task<CheckoutSessionResponse> CreateSubscriptionCheckoutSessionAsync(CreateSubscriptionCheckoutSessionRequest createCheckoutSessionRequest)
    {
        if (!await _stripeService.IsCustomerIdExisted())
        {
            await _stripeService.CreateCustomerAsync();
        }

        return await _stripeService.CreateSubscriptionCheckoutSession(createCheckoutSessionRequest);
    }

    [AuthorizeRoles("Admin")]
    public async Task<RefundResponse> CreateRefundAsync(CreateRefundRequest request)
    {
        return await _stripeService.CreateRefundAsync(request);
    }

    [AuthorizeRoles("Listener,Artist")]
    public async Task<CheckoutSessionResponse> CreateEscrowPaymentCheckoutSessionAsync(CreateEscrowPaymentRequest request)
    {
        if (!await _stripeService.IsCustomerIdExisted())
        {
            await _stripeService.CreateCustomerAsync();
        }

        return await _stripeService.CreateEscrowPaymentCheckoutSessionAsync(request);
    }

    [AuthorizeRoles("Listener")]
    public async Task<EscrowPaymentResponse> ConfirmOrderCompletionAsync(ConfirmOrderCompletionRequest request)
    {
        return await _stripeService.ConfirmOrderCompletionAsync(request);
    }

    [AuthorizeRoles("Admin")]
    public async Task<EscrowPaymentResponse> ReleaseAdvancePaymentAsync(string orderId)
    {
        return await _stripeService.ReleaseAdvancePaymentAsync(orderId);
    }

    [AuthorizeRoles("Admin")]
    public async Task<EscrowPaymentResponse> ReleaseCompletionPaymentAsync(string orderId)
    {
        return await _stripeService.ReleaseCompletionPaymentAsync(orderId);
    }

    //public bool TransferToArtist(string artistAccountId, long amount)
    //{
    //    TransferResponse transferResponse = _stripeService.TransferToArtist(artistAccountId, amount);
    //    return transferResponse != null;
    //}

    //public bool TransferGroupArtist(string[] artistAccountIds, long amount)
    //{
    //    _stripeService.TransferGroupArtist(artistAccountIds, amount);
    //    return true;
    //}
}
