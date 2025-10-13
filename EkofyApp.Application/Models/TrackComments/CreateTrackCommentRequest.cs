namespace EkofyApp.Application.Models.TrackComments;

public sealed record CreateTrackCommentRequest
{
    public string TrackId { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string? ParentCommentId { get; init; }
}