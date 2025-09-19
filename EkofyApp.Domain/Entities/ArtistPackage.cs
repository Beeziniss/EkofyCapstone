using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities
{
    public class ArtistPackage : TimeStamped, IEntityCustom
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public  string PackageName { get; set; } = null!;
        public decimal Price { get; set; }
        public int EstimateDeliveryDays { get; set; }
        public string? Description { get; set; }
        public string ServiceDetails { get; set; } = null!;
        public ArtistPackageStatus Status { get; set; }
    }
}
