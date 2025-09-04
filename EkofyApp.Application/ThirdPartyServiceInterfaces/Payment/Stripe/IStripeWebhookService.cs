namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
public interface IStripeWebhookService
{
    Task HandleWebhookCheckoutSessionAsync(string json, string stripeSignature);
    void HandleWebhookCustomer(string json, string stripeSignature);
    void HandleWebhookExpressConnectedAccount(string json, string stripeSignature);
    Task HandleWebhookSubscriptionPlanAsync(string json, string stripeSignature);
}
