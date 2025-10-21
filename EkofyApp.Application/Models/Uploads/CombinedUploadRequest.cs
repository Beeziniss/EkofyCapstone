using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Works;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Application.Models.Uploads;
public sealed record class CombinedUploadRequest
{
    /// <summary>
    /// Unique identifier for this combined upload request
    /// </summary>
    public string Id { get; init; } = null!;

    /// <summary>
    /// Track information for the upload
    /// </summary>
    public TrackTempRequest Track { get; init; } = null!;

    /// <summary>
    /// Work information for the upload
    /// </summary>
    public WorkTempRequest Work { get; init; } = null!;

    /// <summary>
    /// Recording information for the upload
    /// </summary>
    public RecordingTempRequest Recording { get; init; } = null!;

    /// <summary>
    /// Timestamp when the upload request was created
    /// </summary>
    public DateTimeOffset RequestedAt { get; init; } = HelperMethod.GetUtcPlus7TimeOffset();

    /// <summary>
    /// User ID who created this upload request
    /// </summary>
    public string CreatedBy { get; init; } = null!;
}