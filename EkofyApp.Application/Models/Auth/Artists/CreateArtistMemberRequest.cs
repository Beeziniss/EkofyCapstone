using EkofyApp.Application.Mappers;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Auth.Artists;
public sealed record class CreateArtistMemberRequest : IMapFrom<ArtistMember>
{
    public string FullName { get; init; } = null!; // Full name of the artist member, e.g., "John Doe"
    public string Email { get; init; } = null!; // Email of the artist member, e.g., "
    public string PhoneNumber { get; init; } = null!; // Phone number of the artist member, e.g., "+1234567890"
    public UserGender Gender { get; init; }
}
