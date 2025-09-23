using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Auth.Admins;
public sealed record class AuthAdminTokenResponse
{
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public UserRole Role { get; init; }
}
