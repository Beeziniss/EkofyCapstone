using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Artist : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the artist
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user associated with the artist
    public string Name { get; set; } = null!; // Name of the artist, e.g., "John Doe"

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> CategoryIds { get; set; } = []; // Genre of the artist, e.g., "Pop", "Rock", etc.

    public string? Biography { get; set; } // Biography of the artist, e.g., "John Doe is a singer-songwriter from..."
    public long Followers { get; set; } = default; // Number of followers the artist has
    public long Popularity { get; set; } = default; // Popularity score of the artist
    public string? AvatarImage { get; set; } // URL to the artist's avatar image
    public string? BannerImage { get; set; } // URL to the artist's banner image

    public bool IsActive { get; set; } = false;

    public bool IsVerified { get; set; } = false; // Indicates if the artist is verified (Sound Better platform)
    public DateTime? VerifiedAt { get; set; } // Date when the artist was verified

    public IdentityCard IdentityCard { get; set; } = null!;

    public Restriction? Restriction { get; set; }
}
