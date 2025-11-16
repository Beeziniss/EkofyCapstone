namespace EkofyApp.Application.Models.Comments;

public sealed record UpdateTrackCommentRequest
{
    public string CommentId { get; init; } = null!;
    public string Content { get; init; } = null!;
}