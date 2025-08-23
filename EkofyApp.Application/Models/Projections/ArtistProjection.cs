using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Application.Models.Projections;
public sealed class ArtistProjection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the artist
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user associated with the artist
    public string Name { get; set; } = null!; // Name of the artist, e.g., "John Doe"
    public string Email { get; set; } = null!; // Email of the artist, e.g., "

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> CategoryIds { get; set; } = []; // Genre of the artist, e.g., "Pop", "Rock", etc.

    public string? Biography { get; set; } // Biography of the artist, e.g., "John Doe is a singer-songwriter from..."
    public long Followers { get; set; } = default; // Number of followers the artist has
    public long Popularity { get; set; } = default; // Popularity score of the artist
    public string? AvatarImage { get; set; } // URL to the artist's avatar image
    public string? BannerImage { get; set; } // URL to the artist's banner image

    public bool IsVerified { get; set; } = false; // Indicates if the artist is verified (Sound Better platform)
    public DateTimeOffset? VerifiedAt { get; set; } // Date when the artist was verified

    public IdentityCard IdentityCard { get; set; } = null!;

    public Restriction Restriction { get; set; } = null!;
}
