using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Tracks;
public sealed record TrackTempResponse
{
    #region Request
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }

    public TrackType Type { get; init; }
    public List<string> MainArtistIds { get; init; } = [];
    public List<string> FeaturedArtistIds { get; init; } = [];
    public List<string> CategoryIds { get; init; } = [];
    public List<string> Tags { get; init; } = [];

    public string CoverImage { get; init; } = null!;
    public string? PreviewVideo { get; init; }
    public bool IsExplicit { get; init; }
    public string? Lyrics { get; init; }

    public ReleaseInfo ReleaseInfo { get; init; } = null!;

    public List<LegalDocument> LegalDocuments { get; set; } = [];

    public string CreatedBy { get; init; } = null!;
    #endregion

    //public AudioFingerprint AudioFingerprint { get; init; } = null!;
    public AudioFeature AudioFeature { get; init; } = null!;
    public string AlternativeDescription { get; init; } = null!;

    public float[] EmbeddingVector { get; set; } = null!;
}
