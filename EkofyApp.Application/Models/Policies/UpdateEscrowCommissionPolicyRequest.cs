using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Policies;
public sealed record class UpdateEscrowCommissionPolicyRequest
{
    public long Version { get; init; }
    public decimal? PlatformFeePercentage { get; init; }
    public CurrencyType? Currency { get; init; }
}
