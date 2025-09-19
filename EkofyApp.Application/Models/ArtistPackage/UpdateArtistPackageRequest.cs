namespace EkofyApp.Application.Models.ArtistPackage
{
    public class UpdateArtistPackageRequest
    {
        public string Id { get; set; } = null!;
        public string PackageName { get; set; } = null!;
        public decimal Price { get; set; }
        public int EstimateDeliveryDays { get; set; }
        public string? Description { get; set; }
        public string ServiceDetails { get; set; } = null!;
    }
}
