namespace EkofyApp.Application.Models.Messages;

public sealed record class MessageResponse
{
    public string Nickname { get; set; } = null!;
    public string Avatar { get; set; } = null!;
}
