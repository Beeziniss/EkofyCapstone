using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.Enums.Subcriptions;
using Stripe;
using CheckoutSession = Stripe.Checkout.Session;
using PortalSession = Stripe.BillingPortal.Session;
using PortalSessionService = Stripe.BillingPortal.SessionService;
using StripeInvoice = Stripe.Invoice;
using StripeSubscription = Stripe.Subscription;
using Subscription = EkofyApp.Domain.Entities.Subscription;

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
