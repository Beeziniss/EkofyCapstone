using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Comments;

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
    
    // User Information
    public CommenterInfo Commenter { get; init; } = null!;
}

public sealed record CommenterInfo
{
    public string UserId { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string Avatar { get; init; } = null!;
    public string Email { get; init; } = null!;
    public UserRole Role { get; init; }
    public bool IsVerified { get; init; }
    
    // Listener-specific info (if applicable)
    public ListenerInfo? Listener { get; init; }
    
    // Artist-specific info (if applicable)  
    public ArtistInfo? Artist { get; init; }
}

public sealed record ListenerInfo
{
    public string Id { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string? AvatarImage { get; init; }
    public bool IsVerified { get; init; }
    public long FollowerCount { get; init; }
}

public sealed record ArtistInfo
{
    public string Id { get; init; } = null!;
    public string StageName { get; init; } = null!;
    public string? AvatarImage { get; init; }
    public bool IsVerified { get; init; }
    public long FollowerCount { get; init; }
    public long Popularity { get; init; }
}