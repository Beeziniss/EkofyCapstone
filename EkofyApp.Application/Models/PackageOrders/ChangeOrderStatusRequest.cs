using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.PackageOrders
{
    public sealed record ChangeOrderStatusRequest
    {
        public string Id { get; init; } = default!;
        public PackageOrderStatus Status { get; init; }
        public string? Reason { get; init; }
    }
}
