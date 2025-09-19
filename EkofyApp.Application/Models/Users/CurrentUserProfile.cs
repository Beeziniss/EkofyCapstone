using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Users;
public sealed record class CurrentUserProfile
{
    public string UserId { get; init; } = null!;
    public string? ListenerId { get; init; }
    public string? ArtistId { get; init; }
    public UserRole Role { get; init; }
}
