using EkofyApp.Domain.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Listener : TimeStamped
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the listener
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!; // Unique identifier for the user associated with the listener

    public string DisplayName { get; set; } = null!; // DisplayName of the listener, e.g., "John Doe"
    public string DisplayNameUnsigned { get; set; } = null!; // Unsign version of the listener's display name for search optimization
    public string Email { get; set; } = null!; // Email of the listener, e.g., "
    public string? AvatarImage { get; set; } // URL to the listener's avatar image
    public string? BannerImage { get; set; } // URL to the listener's banner image

    public bool IsVerified { get; set; } = false; // Indicates if the listener is verified
    public DateTimeOffset? VerifiedAt { get; set; } // Date when the listener was verified

    public long FollowerCount { get; set; } = default; // Number of followers the listener has
    public long FollowingCount { get; set; } = default; // Number of artists the listener is following
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> LastFollowers { get; set; } = []; // List of last followers, storing their IDs
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> LastFollowings { get; set; } = []; // List of last following artists, storing their IDs

    public bool IsVisible { get; set; } = true; // Indicates if it is visible to users
}
