using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class OneOffSnapshot
{
    public string PackageName { get; set; } = null!;
    public decimal PackageAmount { get; set; }
    public CurrencyType PackageCurrency { get; set; }
    public int EstimateDeliveryDays { get; set; }
    public string? Description { get; set; }
    // TODO: Thêm ServiceDetails -> Metadata
    public ArtistPackageStatus Status { get; set; }
}
