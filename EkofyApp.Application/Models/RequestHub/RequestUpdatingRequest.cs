namespace EkofyApp.Application.Models.RequestHub
{
    public record RequestUpdatingRequest
    {
        public string Id { get; set; } = null!;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public List<string>? Attachments { get; set; }
        public bool? IsClosed { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
