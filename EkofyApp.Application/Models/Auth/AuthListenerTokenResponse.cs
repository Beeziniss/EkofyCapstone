using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Auth;
public sealed record class AuthListenerTokenResponse
{
    public string AccessToken { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string ListenerId { get; init; } = null!;
    public List<UserRole> Roles { get; init; } = [];
}
