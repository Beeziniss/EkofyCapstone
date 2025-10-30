using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Policies;
public sealed record class UpdateRoyalPolicyRequest
{
    public long Version { get; init; }
    public decimal? RatePerStream { get; init; }
    public CurrencyType? Currency { get; init; }
    public decimal? RecordingPercentage { get; init; }
    public decimal? WorkPercentage { get; init; }
}
