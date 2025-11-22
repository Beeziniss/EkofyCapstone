using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Comments;

public sealed record ThreadedCommentsRequest
{
    public string TargetId { get; init; } = null!;
    public CommentType CommentType { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public CommentSortOrder SortOrder { get; init; } = CommentSortOrder.ThreadActivity;
}

public sealed record CommentRepliesRequest
{
    public string CommentId { get; init; } = null!;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public CommentSortOrder SortOrder { get; init; } = CommentSortOrder.Chronological;
}

public sealed record CommentThreadRequest
{
    public string CommentId { get; init; } = null!;
    public bool IncludeDeleted { get; init; } = false;
}

public enum CommentSortOrder
{
    Chronological,      // Sort by creation time
    ThreadActivity,     // Sort by last activity in thread
    PopularityBased,    // Sort by reply count
    ReverseChronological // Newest first
}