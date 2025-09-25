using EkofyApp.Domain.Base;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities
{
    public class ArtistPackage : TimeStamped, IEntityCustom
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; } = null!;
        public string PackageName { get; set; } = null!;
        public decimal Amount { get; set; }
        public CurrencyType Currency { get; set; } = CurrencyType.vnd;
        public int EstimateDeliveryDays { get; set; }
        public string? Description { get; set; }
        public List<Metadata> ServiceDetails { get; set; } = [];
        public ArtistPackageStatus Status { get; set; }

        public long Version { get; set; }
    }
}
