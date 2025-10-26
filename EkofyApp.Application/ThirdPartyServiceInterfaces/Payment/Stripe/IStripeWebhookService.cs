namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
public interface IStripeWebhookService
{
    Task HandleWebhookCheckoutSessionAsync(string json, string stripeSignature);
    Task HandleWebhookCustomerAsync(string json, string stripeSignature);
    void HandleWebhookExpressConnectedAccount(string json, string stripeSignature);
    Task HandleWebhookInvoiceAsync(string json, string stripeSignature);
    Task HandleWebhookPayoutAsync(string json, string stripeSignature);
    Task HandleWebhookRefundAsync(string json, string stripeSignature);

    // Phương thức để xử lý idempotency - tránh xử lý sự kiện trùng lặp
}
