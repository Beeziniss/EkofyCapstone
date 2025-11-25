namespace EkofyApp.Application.Models.Conversations;

public sealed record class ConversationResponse
{
    public string? ListenerId { get; init; }
    public string? ArtistId { get; init; }
    public string Nickname { get; init; } = null!;
    public string Avatar { get; init; } = null!;
}
