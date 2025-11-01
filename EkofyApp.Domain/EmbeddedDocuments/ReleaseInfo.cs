using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class ReleaseInfo
{
    public bool IsRelease { get; set; } // Indicates if the track is public or private
    public DateTimeOffset? ReleaseDate { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public ReleaseStatus ReleaseStatus { get; set; }
}
