using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.Requests
{
    public sealed record CreateDirectRequest
    {
        public string? PublicRequestId { get; init; }
        public string ArtistId { get; init; } = null!;
        public string? Requirements { get; set; }
        public string PackageId { get; init; } = null!;
    }
}
