namespace EkofyApp.Application.Models.Subscriptions;
public sealed record class DeleteEntitlementsSubscriptionRequest
{
    public string SubscriptionId { get; init; } = string.Empty;
    public List<string> Codes { get; init; } = [];
}
