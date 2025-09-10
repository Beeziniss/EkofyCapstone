using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;

namespace EkofyApp.Api.GraphQL.Query.Payment.Stripes;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class StripeQuery(IStripeService stripeService)
{
    private readonly IStripeService _stripeService = stripeService;

    //public Balance GetBalance()
    //{
    //    return _stripeService.GetBalance();
    //}

    //public async Task<PortalSession> CreateCustomerPortalSessionAsync(string returnUrl)
    //{
    //    return await _stripeService.CreateCustomerPortalSessionAsync(returnUrl);
    //}
}
