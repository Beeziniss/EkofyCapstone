using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Listeners;
public sealed record class UpdateListenerRequest
{
    public string? DisplayName { get; init; }
    public string? AvatarImage { get; init; }
    public string? BannerImage { get; init; }

    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? FullName { get; init; }
    public UserGender? Gender { get; init; }
    public DateTimeOffset? BirthDate { get; init; }
}
