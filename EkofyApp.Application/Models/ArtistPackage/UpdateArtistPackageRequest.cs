using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.Models.ArtistPackage;

public sealed record class UpdateArtistPackageRequest
{
    public string Id { get; init; } = null!;
    public string? PackageName { get; init; }
    public string? Description { get; init; }
}
