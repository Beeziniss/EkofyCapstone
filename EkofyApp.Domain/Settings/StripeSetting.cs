namespace EkofyApp.Domain.Settings;
public sealed record class StripeSetting
{
    public required string CustomerSigningSecret { get; init; }
    public required string ExpressConnectedAccountSigningSecret { get; init; }
    public required string CheckoutSessionSigningSecret { get; init; }
    public required string InvoiceSigningSecret { get; init; }
    public required string InvoicePaymentSigningSecret { get; init; }
    public required string PayoutSigningSecret { get; init; }
    public required string RefundSigningSecret { get; init; }
}
