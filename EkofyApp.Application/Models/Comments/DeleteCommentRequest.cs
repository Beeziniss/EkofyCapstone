namespace EkofyApp.Application.Models.Comments;

public sealed record DeleteCommentRequest
{
    public string CommentId { get; init; } = null!;
}