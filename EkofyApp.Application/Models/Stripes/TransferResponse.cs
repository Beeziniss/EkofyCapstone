namespace EkofyApp.Application.Models.Stripes;
public sealed record class TransferResponse
{
    public string Id { get; init; } = null!;

    public long Amount { get; init; }
    public string Currency { get; init; } = null!;

    public string DestinationAccountId { get; init; } = null!;
    public string Description { get; init; } = null!;

    public DateTime Created { get; init; }
}
