using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class MonthlyFavoriteCount
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!;

    public int Month { get; set; }
    public int Year { get; set; }

    public long FavoriteCount { get; set; } = 0;

    public DateTimeOffset CreatedAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset();
    public DateTimeOffset? ProcessedAt { get; set; }
}
