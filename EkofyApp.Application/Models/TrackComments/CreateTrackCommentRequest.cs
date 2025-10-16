using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.TrackComments;

public sealed record CreateTrackCommentRequest
{
    public string TargetId { get; init; } = null!;
    public CommentType CommentType { get; init; }
    public string Content { get; init; } = null!;
    public string? ParentCommentId { get; init; }
}