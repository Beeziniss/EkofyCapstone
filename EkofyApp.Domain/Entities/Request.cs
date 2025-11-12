using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities
{
    public sealed class Request : IEntityCustom
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!; // Unique identifier for the recording
        [BsonRepresentation(BsonType.ObjectId)]
        public string RequestUserId { get; set; } = null!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PackageId { get; set; }
        public string? Title { get; set; }
        public string? TitleUnsigned { get; set; }
        public string? Summary { get; set; }
        public string? SummaryUnsigned { get; set; }
        public string? DetailDescription { get; set; }
        public string? Requirements { get; set; } // cho direct request
        public RequestBudget Budget { get; set; } = null!;
        public DateTimeOffset? PostCreatedTime { get; set; } // public request
        public DateTimeOffset? UpdatedAt { get; set; } // public request
        public RequestType Type { get; set; }
        public CurrencyType Currency { get; set; } = CurrencyType.vnd;
        public DateTimeOffset Deadline { get; set; }
        public RequestStatus Status { get; set; }
        public DateTimeOffset? RequestCreatedTime { get; set; } // cuar request chung
    }
}
