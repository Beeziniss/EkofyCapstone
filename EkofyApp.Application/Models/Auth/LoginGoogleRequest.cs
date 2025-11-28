namespace EkofyApp.Application.Models.Auth;

public sealed record class LoginGoogleRequest
{
    public string GoogleToken { get; init; } = null!;
    public bool IsMobile { get; init; } = false;
}
