using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Auth.Artists;
public sealed record class AuthArtistTokenResponse
{
    public string AccessToken { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string ArtistId { get; init; } = null!;
    public UserRole Role { get; init; }
    public string AvatarImage { get; init; } = null!;
}
