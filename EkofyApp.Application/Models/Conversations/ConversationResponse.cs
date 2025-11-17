namespace EkofyApp.Application.Models.Conversations;

public sealed record class ConversationResponse
{
    public string Nickname { get; set; } = null!;
    public string Avatar { get; set; } = null!;
}
