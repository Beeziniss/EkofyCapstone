using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums.Users;
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

    public string FullName { get; set; } = null!;
    public string FullNameUnsigned { get; set; } = null!; // Full name without accents for searching
    public UserGender Gender { get; set; }
    public DateTimeOffset BirthDate { get; set; }
    public UserRole Role { get; set; } // "Listener","Artist","Admin","Moderator"

    public string? PhoneNumber { get; set; } // Optional phone number for the user

    public UserStatus Status { get; set; } = UserStatus.Inactive; // Default status is Inactive
    public bool IsLinkedWithGoogle { get; set; }

    public string? StripeCustomerId { get; set; } // Stripe Customer ID for payment processing
    public string? StripeAccountId { get; set; } // Stripe Account ID for artists

    public string? FCMToken { get; set; } // Firebase Cloud Messaging token for push notifications

    public DateTimeOffset? LastLoginAt { get; set; }

    public List<Restriction> Restrictions { get; set; } = []; // Optional restriction details for user

    // TODO: Giả định nếu user không muốn dùng app để nghe nhạc
    // Mà chỉ muốn dùng app để mua gói của nghệ sĩ để yêu cầu nghệ sĩ viết / sáng tác / remix / ... thì sao
    // Thì có thể không cần có các thông tin liên quan đến listeners
    // Resolved: Chả ảnh hưởng vì thông tin của listner phần lớn là từ user
    // Chỉ cần đăng ký tài khoản là mặc định có thể trở thành listener
    // Mà không cần phải có thông tin gì đặc biệt cả
    // Bàng cách này có thể giải quyết được là user đó vừa có thể nghe nhạc vừa có thể mua gói của nghệ sĩ
}
