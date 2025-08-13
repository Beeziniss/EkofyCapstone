using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Auth.Artists;
public sealed record class ArtistRegisterRequest
{
    // User registration details
    public string Email { get; init; } = default!; // User's email address, e.g., "
    public string Password { get; init; } = default!; // User's password, e.g., "P@ssw0rd123"
    public string ConfirmPassword { get; init; } = default!; // Confirmation of the user's password, e.g., "P@ssw0rd123"
    public DateTime BirthDate { get; init; } // User's birth date, e.g., "
    public UserGender Gender { get; init; }

    // For the artist profile
    public string Name { get; init; } = default!; // Name of the artist
    public CreateIdentityCardRequest IdentityCard { get; init; } = null!; // Identity card details for the artist
}
