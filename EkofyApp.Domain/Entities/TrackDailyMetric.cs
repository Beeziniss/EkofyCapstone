using EkofyApp.Domain.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;

public sealed class TrackDailyMetric : TimeStamped
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    [BsonRepresentation(BsonType.ObjectId)]
    public string TrackId { get; set; } = null!;

    // Streaming
    public long StreamCount { get; set; } = default;
    public long DownloadCount { get; set; } = default;

    // Engagement
    public long FavoriteCount { get; set; } = default;
    public long CommentCount { get; set; } = default;
}
