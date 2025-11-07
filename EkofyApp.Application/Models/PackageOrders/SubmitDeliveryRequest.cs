namespace EkofyApp.Application.Models.PackageOrders
{
    public sealed record SubmitDeliveryRequest
    {
        public string PackageOrderId { get; init; } = default!;
        public string DeliveryFileUrl { get; init; } = default!;
        public string? Notes { get; init; }
    }
}
