using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.ArtistPackage;

public sealed record class PendingArtistPackageResponse
{
    public string Id { get; init; } = null!;
    public string ArtistId { get; init; } = null!;
    public string PackageName { get; init; } = null!;
    public decimal Amount { get; init; }
    public CurrencyType Currency { get; init; } = CurrencyType.vnd;
    public int EstimateDeliveryDays { get; init; }
    public string? Description { get; init; }
    public List<Metadata> ServiceDetails { get; init; } = [];
    public ArtistPackageStatus Status { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public TimeSpan? TimeToLive { get; init; } // TTL remaining in Redis
}