using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class AppliedEntitlement
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string EntitlementId { get; set; } = null!; // Unique identifier for the feature
    public string Code { get; set; } = null!; // Unique code for the feature, e.g., "advanced_analytics"
    public EntitlementValueType ValueType { get; set; } // Type of the feature value, e.g., String, Number, Boolean
    public object? Value { get; set; } // Value of the feature, can be a string, number, or boolean depending on the feature type
}
