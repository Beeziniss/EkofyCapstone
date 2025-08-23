using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Tracks;

public sealed record class CreateTrackRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; } = null;

    public List<string> MainArtistIds { get; set; } = [];
    public List<string> FeaturedArtistIds { get; init; } = [];

    public List<string> CategoryIds { get; init; } = [];
    public List<string> Tags { get; init; } = [];

    public string CoverImage { get; init; } = default!;
    public string? PreviewVideo { get; init; }

    public bool IsExplicit { get; init; }
    public string? Lyrics { get; init; }

    public bool IsReleased { get; init; }
    public DateTimeOffset? ReleaseDate { get; init; }
    public ReleaseStatus ReleaseStatus { get; init; }
}
