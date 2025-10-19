using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.ApprovalHistories;
public sealed record class ApprovalHistoryRequest
{
    public string? TargetOwnerId { get; set; } // e.g. owner of the track/album
    public string TargetId { get; set; } = null!; // e.g. userId, trackId
    public ApprovalType ApprovalType { get; set; } // e.g. "Artist", "Track", "Album"
    public string ActionByUserId { get; set; } = null!;
    public DateTimeOffset ActionAt { get; set; }
    public HistoryActionType Action { get; set; } // "Approve", "Reject", "RequestChange", etc.
    public string? Notes { get; set; }
    public object Snapshot { get; set; } = null!;
}
