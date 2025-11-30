using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.EmbeddedDocuments
{
    public class CustomArtistPackageInfo
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? ConversationId { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string ClientId { get; set; } = null!;
    }
}
