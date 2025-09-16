namespace EkofyApp.Application.Models.RequestHub
{
    public sealed record CreateRequestCommentRequest
    {
        public string RequestId { get; set; } = null!;
        public string SenderId { get; set; } = null!;
        public string Content { get; set; } = null!;
    }
}
