using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Artist;
using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Artists;

public sealed record class PendingArtistRegistrationRequest
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string PasswordHash { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public DateTimeOffset BirthDate { get; init; }
    public UserGender Gender { get; init; }
    public string PhoneNumber { get; init; } = null!;
    
    // Artist specific information
    public string StageName { get; init; } = null!;
    public ArtistType ArtistType { get; init; }
    public string? AvatarImage { get; init; }
    public List<ArtistMember> Members { get; init; } = [];
    public IdentityCard IdentityCard { get; init; } = null!;
    
    public DateTimeOffset RequestedAt { get; init; }
}