namespace EkofyApp.Application.Models.PackageOrders
{
    public sealed record PackageOrderRefundRequest
    {
        public string Id { get; init; } = default!;
        public int ArtistPercentageAmount { get; init; }
        public int RequestorPercentageAmount { get; init; }
    }
}
