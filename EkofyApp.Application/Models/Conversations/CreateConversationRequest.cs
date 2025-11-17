namespace EkofyApp.Application.Models.Conversations
{
    public sealed record CreateConversationRequest
    {
        public string OtherUserId { get; init; } = null!;
        public string RequestHubId { get; init; } = null!;
    }
}
