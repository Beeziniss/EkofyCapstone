namespace EkofyApp.Application.Models.TrackComments;

public sealed record DeleteTrackCommentRequest
{
    public string CommentId { get; init; } = null!;
}