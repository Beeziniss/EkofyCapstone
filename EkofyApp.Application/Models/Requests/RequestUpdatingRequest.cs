using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Requests
{
    public record RequestUpdatingRequest
    {
        public string Id { get; init; } = null!;
        public string? Title { get; init; }
        public string? Summary { get; init; }
        public string? DetailDescription { get; init; }
        public int? Duration { get; init; }
        public RequestBudget? Budget { get; init; }
        public RequestStatus? Status { get; init; }
    }
}
