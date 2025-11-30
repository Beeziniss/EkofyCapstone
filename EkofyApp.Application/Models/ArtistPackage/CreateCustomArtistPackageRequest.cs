using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.ArtistPackage
{
    public class CreateCustomArtistPackageRequest
    {
        public string PackageName { get; init; } = null!;
        public string ArtistId { get; init; } = null!;
        public string ConversationId { get; init; } = null!;
        public string ClientId { get; init; } = null!;
        public decimal Amount { get; init; }
        public int EstimateDeliveryDays { get; init; }
        public string? Description { get; init; }
        public int MaxRevision { get; init; }
        public List<Metadata> ServiceDetails { get; init; } = [];

    }
}
