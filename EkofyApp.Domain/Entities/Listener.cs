using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Listener : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the listener

    public string Name { get; set; } = null!; // Name of the listener, e.g., "John Doe"
    public string? AvatarImage { get; set; } // URL to the listener's avatar image
    public string? BannerImage { get; set; } // URL to the listener's banner image

    public string? Email { get; set; } // Email address of the listener

    public UserRole Role { get; set; } = UserRole.Listener; // Role of the user, default is Listener
    public SubscriptionType SubscriptionType { get; set; } = SubscriptionType.Free; // Subscription type of the listener

    public bool IsVerified { get; set; } = false; // Indicates if the listener is verified
    public DateTime? VerifiedAt { get; set; } // Date when the listener was verified

    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> Followers { get; set; } = [];
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> Following { get; set; } = [];

    public bool IsLinkedWithGoogle { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
