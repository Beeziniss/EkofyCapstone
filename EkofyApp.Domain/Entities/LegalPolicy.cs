using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class LegalPolicy : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!; // HTML/Markdown
    public long Version { get; set; }
    public PolicyStatus Status { get; set; }

    public DateTimeOffset EffectiveAt { get; set; }
}
