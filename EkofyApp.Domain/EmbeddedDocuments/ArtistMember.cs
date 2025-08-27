using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class ArtistMember
{
    public string FullName { get; set; } = null!; // Full name of the artist member, e.g., "Jane Doe"
    public string Email { get; set; } = null!; // Email of the artist member, e.g., "
    public string PhoneNumber { get; set; } = null!; // Phone number of the artist member, e.g., "+1234567890"
    public bool IsLeader { get; set; } = false; // Indicates if the member is the leader of the artist group
    public UserGender Gender { get; set; }
}
