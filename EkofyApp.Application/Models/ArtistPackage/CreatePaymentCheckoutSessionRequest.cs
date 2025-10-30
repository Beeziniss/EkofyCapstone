using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.ArtistPackage;
public sealed record class CreatePaymentCheckoutSessionRequest
{
    public string PackageId { get; init; } = null!;

    public string SuccessUrl { get; init; } = null!;
    public string CancelUrl { get; init; } = null!;

    public bool IsSavePaymentMethod { get; init; } = false;
    public bool IsReceiptEmail { get; init; } = false;

    // Package Order
    public string ConversationId { get; init; } = null!;
    public string? PackageOrderDescription { get; set; }
    public List<string> RequirementFiles { get; set; } = [];
    public List<PackageOrderDelivery> Deliveries { get; set; } = [];
    public DateTimeOffset Deadline { get; set; }
}
