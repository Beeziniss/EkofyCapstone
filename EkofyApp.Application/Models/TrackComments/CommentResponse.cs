using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.TrackComments;

public sealed record CommentResponse
{
    public string Id { get; init; } = null!;
    public string TargetId { get; init; } = null!;
    public CommentType CommentType { get; init; }
    public string CommenterId { get; init; } = null!;
    public string Content { get; init; } = null!;
    
    // Hierarchical properties
    public string? ParentCommentId { get; init; }
    public string? RootCommentId { get; init; }
    public List<string> ThreadPath { get; init; } = [];
    public int Depth { get; init; }
    public long ReplyCount { get; init; }
    public long TotalRepliesCount { get; init; }
    
    // Metadata
    public bool IsEdited { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset ThreadUpdatedAt { get; init; }
}