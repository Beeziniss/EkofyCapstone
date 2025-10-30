using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.CompilerServices;

namespace EkofyApp.Domain.EmbeddedDocuments
{
    public sealed class TopTrackInfo
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string TrackId { get; set; } = null!;
        public int PlayedCount { get; set; }
    }
}
