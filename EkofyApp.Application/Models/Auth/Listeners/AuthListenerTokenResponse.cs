using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Auth.Listeners;
public sealed record class AuthListenerTokenResponse
{
    public string AccessToken { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string ListenerId { get; init; } = null!;
    public UserRole Role { get; init; }
}
