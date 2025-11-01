using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.RequestHub
{
    public record RequestUpdatingRequest
    {
        public string Id { get; init; } = null!;
        public string? Title { get; init; }
        public string? Summary { get; init; }
        public string? DetailDescription { get; init; }
        public DateTime? Deadline { get; init; }
        public RequestBudget? Budget { get; init; }
        public RequestStatus? Status { get; init; }
    }
}
