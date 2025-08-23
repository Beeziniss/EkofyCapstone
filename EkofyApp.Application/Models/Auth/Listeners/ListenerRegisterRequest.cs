using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Auth.Listeners;
public sealed record class ListenerRegisterRequest
{
    // User registration details
    public string Email { get; init; } = default!; // User's email address, e.g., "
    public string Password { get; init; } = default!; // User's password, e.g., "P@ssw0rd123"
    public string ConfirmPassword { get; init; } = default!; // Confirmation of the user's password, e.g., "P@ssw0rd123"
    public DateTimeOffset BirthDate { get; init; } // User's birth date, e.g., "
    public UserGender Gender { get; init; }

    // For the listener profile
    public string Name { get; init; } = default!; // Name of the listener, e.g., "John Doe"
}
