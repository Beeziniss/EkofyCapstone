namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class PackageOrderDelivery
{
    public string DeliveryFileUrl { get; set; } = null!;
    public string? Notes { get; set; }
    public int RevisionNumber { get; set; }
    public string? ClientFeedback { get; set; }
    public DateTimeOffset? RequestedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
}
