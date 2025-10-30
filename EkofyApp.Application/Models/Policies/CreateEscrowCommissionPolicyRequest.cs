using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Policies;
public sealed record class CreateEscrowCommissionPolicyRequest
{
    public CurrencyType Currency { get; init; } = CurrencyType.vnd;
    public decimal PlatformFeePercentage { get; init; }
}
