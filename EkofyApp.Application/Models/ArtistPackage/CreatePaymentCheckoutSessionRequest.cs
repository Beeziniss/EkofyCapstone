namespace EkofyApp.Application.Models.ArtistPackage;
public sealed record class CreatePaymentCheckoutSessionRequest
{
    public string PackageId { get; init; } = null!;

    public string SuccessUrl { get; init; } = null!;
    public string CancelUrl { get; init; } = null!;

    public bool IsSavePaymentMethod { get; init; } = false;
    public bool IsReceiptEmail { get; init; } = false;
}
