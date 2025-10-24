namespace EkofyApp.Domain.Settings;
public sealed record class StripeSetting
{
    public required string CustomerSigningSecret { get; init; }
    public required string SubscriptionSigningSecret { get; init; }
    public required string AccountV2SigningSecret { get; init; }
    public required string AccountSigningSecret { get; init; }
    public required string CheckoutSessionSigningSecret { get; init; }
    public required string InvoiceSigningSecret { get; init; }
    public required string PayoutSigningSecret { get; init; }
}
