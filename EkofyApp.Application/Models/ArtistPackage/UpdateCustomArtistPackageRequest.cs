using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.ArtistPackage
{
    public class UpdateCustomArtistPackageRequest
    {
        public string Id { get; init; } = null!;
        public string? PackageName { get; init; }
        public decimal? Amount { get; init;}
        public int? EstimateDeliveryDays { get; init;}
        public string? Description { get; init;}
        public List<Metadata>? ServiceDetails { get; init;}
        public int? MaxRevision { get; init;}
    }
}
