using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.ArtistPackage
{
    public class CreateArtistPackageRequest
    {
        public string PackageName { get; set; } = null!;
        public decimal Amount { get; set; }
        public int EstimateDeliveryDays { get; set; }
        public string? Description { get; set; }
        public List<Metadata> ServiceDetails { get; set; } = [];
    }
}
