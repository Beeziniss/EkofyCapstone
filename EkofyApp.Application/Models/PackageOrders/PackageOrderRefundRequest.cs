namespace EkofyApp.Application.Models.PackageOrders
{
    public sealed record PackageOrderRefundRequest
    {
        public string Id { get; init; } = default!;
        public decimal ArtistPercentageAmount { get; init; }
        public decimal RequestorPercentageAmount { get; init; }
    }
}
