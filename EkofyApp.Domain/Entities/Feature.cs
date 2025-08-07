using EkofyApp.Domain.Base;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Feature : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the feature
    public string Name { get; set; } = null!; // Name of the feature, e.g., "Premium Support"
    public string? Description { get; set; } // Description of the feature
    public bool IsEnabled { get; set; } = true; // Indicates if the feature is enabled or not
}
