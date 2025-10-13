namespace EkofyApp.Application.Models.TrackComments;

public sealed record TrackCommentResponse
{
    public string Id { get; init; } = null!;
    public string TrackId { get; init; } = null!;
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
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime ThreadUpdatedAt { get; init; }
    
    // Helper properties
    public bool IsRootComment => string.IsNullOrEmpty(ParentCommentId);
    public string ThreadId => RootCommentId ?? Id;
}