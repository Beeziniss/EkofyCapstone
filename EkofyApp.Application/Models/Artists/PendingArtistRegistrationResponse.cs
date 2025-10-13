using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Artist;
using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Artists;

public sealed record class PendingArtistRegistrationResponse
{
    public string Id { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string StageName { get; init; } = null!;
    public string StageNameUnsigned { get; init; } = null!;
    public ArtistType ArtistType { get; init; }
    public UserGender Gender { get; init; }
    public DateTimeOffset BirthDate { get; init; }
    public string PhoneNumber { get; init; } = null!;
    
    // Artist specific information
    public string? AvatarImage { get; init; }
    public List<ArtistMember> Members { get; init; } = [];
    
    public DateTimeOffset RequestedAt { get; init; }
    public TimeSpan? TimeToLive { get; init; } // TTL remaining in Redis
    
    // Identity Card info for verification
    public string IdentityCardNumber { get; init; } = null!;
    public string IdentityCardFullName { get; init; } = null!;
    public DateTimeOffset IdentityCardDateOfBirth { get; init; }
    public string PlaceOfOrigin { get; init; } = null!;
    public string PlaceOfResidence { get; init; } = null!;
    public string? FrontImageUrl { get; init; }
    public string? BackImageUrl { get; init; }
}