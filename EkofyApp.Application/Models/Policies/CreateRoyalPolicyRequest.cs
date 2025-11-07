using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Policies;
public sealed record class CreateRoyalPolicyRequest
{
    public decimal RatePerStream { get; init; }
    public CurrencyType Currency { get; init; } = CurrencyType.vnd;
    public decimal RecordingPercentage { get; init; }
    public decimal WorkPercentage { get; init; }
    //public bool IsActive { get; init; }
    //public DateTimeOffset EffectiveAt { get; init; }
}
