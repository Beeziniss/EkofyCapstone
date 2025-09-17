using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;

namespace EkofyApp.Api.GraphQL.Mutation.Payment.Stripes;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class StripeMutation(IStripeService stripeService)
{
    private readonly IStripeService _stripeService = stripeService;

    public async Task<AccountLinkResponse> CreateExpressConnectedAccountAsync()
    {
        await _stripeService.CreateExpressConnectedAccount();

        AccountLinkResponse accountLink = _stripeService.CreateAccountOnboardingLink(
            refreshUrl: "https://ekofy.com/reauth",
            returnUrl: "https://ekofy.com/dashboard");

        return accountLink;
    }

    //public async Task<CheckoutSessionResponse> CreatePaymentCheckoutSessionAsync(CreateCheckoutSessionRequest createCheckoutSessionRequest)
    //{
    //    if(!await _stripeService.IsCustomerIdExisted())
    //    {
    //        await _stripeService.CreateCustomerAsync();
    //    }

    //    return await _stripeService.CreatePaymentCheckoutSessionAsync(createCheckoutSessionRequest);
    //}

    public async Task<CheckoutSessionResponse> CreateSubscriotionCheckoutSessionAsync(CreateCheckoutSessionRequest createCheckoutSessionRequest)
    {
        if (!await _stripeService.IsCustomerIdExisted())
        {
            await _stripeService.CreateCustomerAsync();
        }

        return await _stripeService.CreateSubscriptionCheckoutSession(createCheckoutSessionRequest);
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
