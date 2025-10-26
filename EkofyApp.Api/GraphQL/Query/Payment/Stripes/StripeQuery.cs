using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Application.Models.Stripes;

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

    [AuthorizeRoles("Admin")]
    public async Task<RefundResponse?> GetRefundAsync(string refundTransactionId)
    {
        return await _stripeService.GetRefundAsync(refundTransactionId);
    }

    [AuthorizeRoles("Admin")]
    public async Task<List<RefundResponse>> GetRefundsAsync(string? paymentTransactionId = null, int limit = 10, string? startingAfter = null)
    {
        return await _stripeService.ListRefundsAsync(paymentTransactionId, limit, startingAfter);
    }

    [AuthorizeRoles("Listener,Artist,Admin")]
    public async Task<EscrowPaymentResponse> GetEscrowPaymentAsync(string orderId)
    {
        return await _stripeService.GetEscrowPaymentAsync(orderId);
    }

    [AuthorizeRoles("Listener,Artist,Admin")]
    public async Task<List<EscrowPaymentResponse>> GetEscrowPaymentsAsync(string? userId = null, int limit = 10)
    {
        return await _stripeService.ListEscrowPaymentsAsync(userId, limit);
    }
}
