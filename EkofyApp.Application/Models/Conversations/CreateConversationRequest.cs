namespace EkofyApp.Application.Models.Conversations
{
    public sealed record CreateConversationRequest
    {
        public string OtherUserId { get; init; } = null!;
        public string RequestId { get; init; } = null!;
    }
}
