using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.Artists;
public sealed record class CreateArtistRequest
{
    public string UserId { get; init; } = default!; // User ID of the artist, e.g., "user123"
    public string Name { get; init; } = default!; // DisplayName of the artist, e.g., "John Doe"
    public string Biography { get; init; } = default!;
    public IdentityCard IdentityCard { get; init; } = default!; // Identity card information of the artist
}
