namespace EkofyApp.Application.Models.Conversations
{
    public sealed record CreateConversationRequest
    {
        public List<string> UserIds { get; init; } = default!;
        public string? RequestHubId { get; init; }

    }
}
