using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed record class UpdateMetdataSubscriptionRequest
{
    public string SubscriptionId { get; init; } = null!;
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Code { get; init; }

    public decimal? Amount { get; init; }
    public CurrencyType? Currency { get; init; }
}
