namespace EkofyApp.Application.Models.Chat;
public record ConversationRequest
{
    public string? ConversationId { get; set; }
    public string? CurrentUserId { get; set; }
}
