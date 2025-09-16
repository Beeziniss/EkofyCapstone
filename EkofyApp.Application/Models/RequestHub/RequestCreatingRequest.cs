namespace EkofyApp.Application.Models.RequestHub
{
    public sealed record RequestCreatingRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<string>? Attachments { get; set; }
    }
}
