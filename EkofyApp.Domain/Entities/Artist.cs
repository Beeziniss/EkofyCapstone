using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Artist;
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
    public string StageName { get; set; } = null!; // DisplayName of the artist, e.g., "John Doe"
    public string StageNameUnsigned { get; set; } = null!; // Unsign version of the artist's stage name for search optimization
    public string Email { get; set; } = null!; // Email of the artist, e.g., "

    public ArtistType ArtistType { get; set; } // Type of artist, e.g., Individual, Band, etc.
    public List<ArtistMember> Members { get; set; } = []; // List of members in the artist group, if applicable

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> CategoryIds { get; set; } = []; // Genre of the artist, e.g., "Pop", "Rock", etc.

    public string? Biography { get; set; } // Biography of the artist, e.g., "John Doe is a singer-songwriter from..."
    public long FollowerCount { get; set; } = default; // Number of followers the artist has
    public decimal Popularity { get; set; } = default; // Popularity score of the artist
    public string? AvatarImage { get; set; } // URL to the artist's avatar image
    public string? BannerImage { get; set; } // URL to the artist's banner image

    public bool IsVerified { get; set; } = false; // Indicates if the artist is verified (Sound Better platform)
    public DateTimeOffset? VerifiedAt { get; set; } // Date when the artist was verified

    // TODO: Sẽ thêm các vấn đề liên quan đến bản quyền, hợp đồng, v.v. sau
    // Có liên quan đến CCCD của nghệ sĩ, có thể là một đối tượng riêng biệt hoặc nhúng vào đây
    // Resolve the legal documents and restrictions for the artist
    public IdentityCard IdentityCard { get; set; } = null!;
    public List<LegalDocument> LegalDocuments { get; set; } = []; // List of legal documents associated with the artist, e.g., contracts, agreements, etc.

    public bool IsVisible { get; set; } = true; // Indicates if it is visible to users

    // Revenue
    public decimal RoyaltyEarnings { get; set; } = default;
    public decimal ServiceRevenue { get; set; } = default; // Tiền chưa trừ hoa hồng
    public decimal GrossRevenue => RoyaltyEarnings + ServiceRevenue;
    public decimal RefundAmount { get; set; } = default;
}
