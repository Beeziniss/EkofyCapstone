using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Auth.Moderators;
public sealed record class AuthModeratorTokenResponse
{
    public string AccessToken { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public UserRole Role { get; init; }
}
