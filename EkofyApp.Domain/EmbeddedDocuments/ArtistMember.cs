using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class ArtistMember
{
    public string FirstName { get; set; } = null!; // Name of the artist member, e.g., "John Doe"
    public string LastName { get; set; } = null!; // Last name of the artist member, e.g., "Smith"
    public string Email { get; set; } = null!; // Email of the artist member, e.g., "
    public UserGender Gender { get; set; } = UserGender.NotSpecified;
}
