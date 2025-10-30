namespace EkofyApp.Application.Models.RequestHub
{
    public sealed record RequestCreatingRequest
    {
        public string Title { get; init; } = null!;
        public string Summary { get; init; } = null!;
        public string DetailDescription { get; init; } = null!;
        public DateOnly Deadline { get; init; }
        public decimal Budget { get; init; }
    }
}
