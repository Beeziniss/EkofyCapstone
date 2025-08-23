using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class ReleaseInfo
{
    public bool IsReleased { get; set; } // Indicates if the track is public or private
    public DateTimeOffset? ReleaseDate { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public ReleaseStatus ReleaseStatus { get; set; }
}
