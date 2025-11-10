using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities
{
    public sealed class Request : TimeStamped, IEntityCustom
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!; // Unique identifier for the recording

        [BsonRepresentation(BsonType.ObjectId)]
        public string RequestUserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string TitleUnsigned { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public string SummaryUnsigned { get; set; } = null!;
        public string DetailDescription { get; set; } = null!;
        public RequestBudget Budget { get; set; } = null!;
        public CurrencyType Currency { get; set; } = CurrencyType.vnd;
        public DateTimeOffset Deadline { get; set; }
        public RequestStatus Status { get; set; }
    }
}
