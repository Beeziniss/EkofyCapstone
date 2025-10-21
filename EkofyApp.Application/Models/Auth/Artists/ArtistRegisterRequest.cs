using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Artist;
using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Auth.Artists;
public sealed record class ArtistRegisterRequest
{
    // User registration details
    public string Email { get; init; } = default!; // User's email address, e.g., "
    public string Password { get; init; } = default!; // User's password, e.g., "P@ssw0rd123"
    public string ConfirmPassword { get; init; } = default!; // Confirmation of the user's password, e.g., "P@ssw0rd123"
    public string FullName { get; init; } = default!; // User's full name, e.g., "John Doe"
    public DateTimeOffset BirthDate { get; init; } // User's birth date, e.g., "
    public UserGender Gender { get; init; }
    public string PhoneNumber { get; init; } = null!; // Optional phone number for the user

    // Special fields for artist registration
    //public bool IsLegalRepresentative { get; init; } // Indicates if the user is the representative of the artist

    // For the artist profile
    public string StageName { get; init; } = default!; // DisplayName of the artist
    public ArtistType ArtistType { get; init; } // Type of artist, e.g., Individual, Band, etc.
    public string? AvatarImage { get; init; } // Optional avatar image URL for the artist
    public List<CreateArtistMemberRequest> Members { get; init; } = []; // List of members in the artist group, if applicable

    public List<LegalDocument> LegalDocuments { get; set; } = []; // List of legal documents associated with the artist, e.g., contracts, agreements, etc.

    public CreateIdentityCardRequest IdentityCard { get; init; } = null!; // Identity card details for the artist
}
