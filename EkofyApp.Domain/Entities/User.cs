using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class User : Auditable, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; } // If null that means the user log in with Google or Facebook

    public UserGender Gender { get; set; }
    [BsonRepresentation(BsonType.String)]
    public DateTime BirthDate { get; set; }
    public UserRole Role { get; set; } // "Listener","Artist","Admin","Moderator"

    public UserStatus Status { get; set; } = UserStatus.Inactive; // Default status is Inactive
    public bool IsLinkedWithGoogle { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public string? FCMToken { get; set; } // Firebase Cloud Messaging token for push notifications

    public DateTime? LastLoginAt { get; set; }
}
