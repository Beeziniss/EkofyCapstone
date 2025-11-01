namespace EkofyApp.Domain.EmbeddedDocuments
{
    public sealed record RequestBudget
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }
}
