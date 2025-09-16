namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class EntitlementSubscriptionOverride
{
    public string SubscriptionCode { get; set; } = null!;
    public object Value { get; set; } = null!;
}
