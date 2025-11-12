using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.Requests
{
    public sealed record RequestCreatingRequest
    {
        public string Title { get; init; } = null!;
        public string Summary { get; init; } = null!;
        public string DetailDescription { get; init; } = null!;
        public DateTime Deadline { get; init; }
        public RequestBudget Budget { get; init; } = null!;
    }
}
