using EkofyApp.Domain.Base;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class Entitlement : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; } = null!; // Unique identifier for the feature

    public string Name { get; set; } = null!; // DisplayName of the feature, e.g., "Advanced Analytics"
    public string Code { get; set; } = null!; // Unique code for the feature, e.g., "advanced_analytics"
    public string Description { get; set; } = null!; // Description of the feature, e.g., "Access to advanced analytics tools"

    public EntitlementValueType ValueType { get; set; } // Type of the feature value, e.g., String, Number, Boolean
    public object? Value { get; set; } // Value of the feature, can be a string, number, or boolean depending on the feature type

    public DateTimeOffset? ExpiredAt { get; set; } // Optional expiration date for the feature, if applicable
}
