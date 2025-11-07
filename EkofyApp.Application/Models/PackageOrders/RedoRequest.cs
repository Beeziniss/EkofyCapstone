namespace EkofyApp.Application.Models.PackageOrders
{
    public sealed record RedoRequest
    {
        public string PackageOrderId { get; init; } = default!;
        public int RevisionNumber { get; init; }
        public string ClientFeedback { get; init; } = default!;
    }
}
