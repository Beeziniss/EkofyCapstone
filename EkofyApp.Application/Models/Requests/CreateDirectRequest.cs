using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.Requests
{
    public sealed record CreateDirectRequest
    {
        public string? PublicRequestId { get; init; }
        public RequestBudget Budget { get; init; } = null!;
        public DateTimeOffset Deadline { get; init; }
        public string? Requirements { get; set; }
        public string PackageId { get; init; } = null!;
    }
}
