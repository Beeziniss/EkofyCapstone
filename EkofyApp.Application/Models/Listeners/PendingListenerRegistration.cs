using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Listeners;

public sealed record class PendingListenerRegistration
{
    public string Id { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string PasswordHash { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public DateTimeOffset BirthDate { get; init; }
    public UserGender Gender { get; init; }
    
    // Listener specific information
    public string DisplayName { get; init; } = null!;
    public string DisplayNameUnsigned { get; init; } = null!;
    public string? AvatarImage { get; init; }
    
    public DateTimeOffset RequestedAt { get; init; }
}