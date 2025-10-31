using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.RequestHub
{
    public record RequestUpdatingRequest
    {
        public string Id { get; init; } = null!;
        public string? Title { get; init; }
        public string? Summary { get; init; }
        public string? DetailDescription { get; init; }
        public DateOnly? Deadline { get; init; }
        public decimal? Budget { get; init; }
        public RequestStatus? Status { get; init; }
    }
}
