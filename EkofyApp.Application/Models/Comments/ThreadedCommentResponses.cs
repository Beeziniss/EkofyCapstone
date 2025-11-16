namespace EkofyApp.Application.Models.Comments;

public sealed record ThreadedCommentsResponse
{
    public List<CommentThread> Threads { get; init; } = [];
    public int TotalThreads { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage { get; init; }
}

public sealed record CommentThread
{
    public CommentResponse RootComment { get; init; } = null!;
    public List<CommentResponse> Replies { get; init; } = [];
    public int TotalReplies { get; init; }
    public bool HasMoreReplies { get; init; }
    public DateTimeOffset LastActivity { get; init; }
}

public sealed record CommentRepliesResponse
{
    public List<CommentResponse> Replies { get; init; } = [];
    public int TotalReplies { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage { get; init; }
    public string ParentCommentId { get; init; } = null!;
}