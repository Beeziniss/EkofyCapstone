using EkofyApp.Domain.Enums.Coupons;

namespace EkofyApp.Application.Models.Coupons;
public sealed record class CreateCouponRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Code { get; init; } = null!;

    public decimal PercentOff { get; init; }

    public CouponDurationType Duration { get; init; }

    public CouponStatus Status { get; set; }
}
