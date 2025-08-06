using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Base;
public abstract class TimeStamped
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [BsonIgnoreIfNull]
    public DateTime? DeletedAt { get; set; } // Optional, used for soft deletion
}

