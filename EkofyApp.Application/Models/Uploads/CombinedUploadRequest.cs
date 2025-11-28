using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Works;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Application.Models.Uploads;
public sealed record class CombinedUploadRequest
{
    public string Id { get; init; } = null!;
    public TrackTempRequest Track { get; init; } = null!;
    public WorkTempRequest Work { get; init; } = null!;
    public RecordingTempRequest Recording { get; init; } = null!;
    public ApprovalPriorityStatus ApprovalPriority { get; init; } = ApprovalPriorityStatus.Low;
    public DateTimeOffset RequestedAt { get; init; } = HelperMethod.GetUtcPlus7TimeOffset();
    public string CreatedBy { get; init; } = null!;
}