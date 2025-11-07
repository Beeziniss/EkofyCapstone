using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.ArtistPackage
{
    public class CreateArtistPackageRequest
    {
        public string PackageName { get; init; } = null!;
        public string ArtistId { get; init; } = null!;
        public decimal Amount { get; init; }
        public int EstimateDeliveryDays { get; init; }
        public string? Description { get; init; }
        public List<Metadata> ServiceDetails { get; init; } = [];
        public int MaxRevisions { get; init; }
    }
}
