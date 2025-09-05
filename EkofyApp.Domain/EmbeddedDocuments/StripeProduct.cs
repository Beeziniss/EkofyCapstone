namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class StripeProduct
{
    public string Id { get; set; } = null!;
    public List<string> StripePriceIds { get; set; } = [];
}
