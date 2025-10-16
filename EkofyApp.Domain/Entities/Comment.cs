using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace EkofyApp.Domain.Entities;
public sealed class Comment : TimeStamped, IEntityCustom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TargetId { get; set; } = null!;

    public CommentType CommentType { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CommenterId { get; set; } = null!;

    public string Content { get; set; } = null!;

    // Original flat reply support (kept for backward compatibility)
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentCommentId { get; set; }

    // Enhanced hierarchical threading properties
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RootCommentId { get; set; } // Points to the top-level comment in the thread

    public List<string> ThreadPath { get; set; } = []; // Path from root to this comment (e.g., ["root_id", "parent_id"])
    
    public int Depth { get; set; } = 0; // How deep this comment is in the thread (0 = root comment)
    
    public long ReplyCount { get; set; } = 0; // Number of direct replies to this comment
    
    public long TotalRepliesCount { get; set; } = 0; // Total number of replies in this comment's sub-thread

    // Display order properties for proper threading
    public int SortOrder { get; set; } = 0; // Order within the same level
    public DateTimeOffset ThreadUpdatedAt { get; set; } // Last activity in this thread branch

    public bool IsEdited { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    // Helper method to check if this is a root comment
    public bool IsRootComment => string.IsNullOrEmpty(ParentCommentId);

    // Helper method to get the thread identifier for grouping
    public string GetThreadId() => RootCommentId ?? Id;
}