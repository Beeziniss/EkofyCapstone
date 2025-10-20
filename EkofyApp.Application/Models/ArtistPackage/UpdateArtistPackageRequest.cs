using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.ArtistPackage
{
    public class UpdateArtistPackageRequest
    {
        public string Id { get; init; } = null!;
        public string PackageName { get; set; } = null!;
        public string OriginPackageId { get; init; } = null!;
        public decimal Amount { get; set; }
        public int EstimateDeliveryDays { get; set; }
        public string? Description { get; set; }
        public List<Metadata> ServiceDetails { get; set; } = null!;
        public bool IsDelete { get; set; }
    }
}
