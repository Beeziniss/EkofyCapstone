using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;

namespace EkofyApp.Api.GraphQL.Mutation.Payment.Stripes;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class StripeMutation(IStripeService stripeService, IUserSubscriptionService userSubscriptionService)
{
    private readonly IStripeService _stripeService = stripeService;
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;

    public async Task<AccountLinkResponse> CreateCustomConnectedAccountAsync()
    {
        await _stripeService.CreateCustomConnectedAccount();

        AccountLinkResponse accountLink = _stripeService.CreateAccountOnboardingLink(
            refreshUrl: "https://google.com?refresh=1",
            returnUrl: "https://google.com?return-1");

        return accountLink;
    }

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

        // Kiểm trả xem người dùng đã có subscription chưa
        await _userSubscriptionService.VerifyUserSubscriptionAsync();

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
