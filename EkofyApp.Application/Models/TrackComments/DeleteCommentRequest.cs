namespace EkofyApp.Application.Models.TrackComments;

public sealed record DeleteCommentRequest
{
    public string CommentId { get; init; } = null!;
}