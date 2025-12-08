namespace EkofyApp.Application.Models.Users;

public sealed record class SenderProfile
{
    public string Nickname { get; init; } = null!;
    public string? Avatar { get; init; }
}
