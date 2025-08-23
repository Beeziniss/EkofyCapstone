using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Application.Models.Tracks;
public sealed record class TrackTempRequest
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }

    public List<string> MainArtistIds { get; init; } = [];
    public List<string> FeaturedArtistIds { get; init; } = [];
    public List<string> CategoryIds { get; init; } = [];
    public List<string> Tags { get; init; } = [];

    public string CoverImage { get; init; } = null!;
    public string? PreviewVideo { get; init; }
    public bool IsExplicit { get; init; }
    public string? Lyrics { get; init; }

    public ReleaseInfo ReleaseInfo { get; init; } = null!;

    public string CreatedBy { get; init; } = null!;

    public DateTimeOffset RequestedAt { get; init; } = HelperMethod.GetUtcPlus7TimeOffset();
}
