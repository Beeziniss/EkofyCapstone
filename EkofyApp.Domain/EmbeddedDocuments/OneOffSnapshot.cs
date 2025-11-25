using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class OneOffSnapshot
{
    // Artist Package
    public string PackageName { get; set; } = null!;
    public decimal PackageAmount { get; set; }
    public CurrencyType PackageCurrency { get; set; } = CurrencyType.vnd;
    public int EstimateDeliveryDays { get; set; }
    public string? PackageDescription { get; set; }
    public int MaxRevision { get; set; }
    public List<Metadata> ServiceDetails { get; set; } = [];
    public ArtistPackageStatus ArtistPackageStatus { get; set; }

    // Package Order
    public int Duration { get; set; }
    public decimal PlatformFeePercentage { get; set; }
    public decimal ArtistFeePercentage { get; set; }

    public OneOffType OneOffType { get; set; }
}
