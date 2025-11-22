namespace EkofyApp.Application.Models.Chat;

public sealed record class ChatMessageRequest
{
    public string? ConversationId { get; set; }
    public string SenderId { get; init; } = null!;
    public string ReceiverId { get; init; } = null!;
    public string Text { get; init; } = null!;
    public string? Url { get; init; }
}
