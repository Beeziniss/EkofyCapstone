using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.ArtistPackage;
public sealed record class CreatePaymentCheckoutSessionRequest
{
    public string PackageId { get; init; } = null!;
    public string RequestId { get; init; } = null!;

    public string SuccessUrl { get; init; } = null!;
    public string CancelUrl { get; init; } = null!;

    public bool IsSavePaymentMethod { get; init; } = false;
    public bool IsReceiptEmail { get; init; } = false;
    //Request
    public string Requirements { get; init; } = null!;

    // Package Order
    public string? ConversationId { get; init; }
    public List<PackageOrderDelivery> Deliveries { get; set; } = [];
    public int Duration { get; set; }
}
