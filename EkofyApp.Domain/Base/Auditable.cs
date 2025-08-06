using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Base;
public abstract class Auditable : TimeStamped
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CreatedBy { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? UpdatedBy { get; set; }
}

