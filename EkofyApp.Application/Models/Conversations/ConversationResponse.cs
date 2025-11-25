namespace EkofyApp.Application.Models.Conversations;

public sealed record class ConversationResponse
{
    public string? ListenerId { get; set; }
    public string? ArtistId { get; set; }
    public string Nickname { get; set; } = null!;
    public string Avatar { get; set; } = null!;
}
