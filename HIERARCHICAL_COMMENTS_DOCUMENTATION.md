# Hierarchical Comment System for Track Comments

## Overview
This implementation provides a multi-level comment threading system similar to Reddit/Facebook, supporting nested replies with unlimited depth.

## Key Features

### 1. **Multi-level Threading**
- **Root Comments**: Top-level comments (Depth = 0)
- **Nested Replies**: Comments can reply to other comments at any depth
- **Thread Path**: Each comment maintains a path showing its ancestry
- **Thread Grouping**: All comments in a thread are linked via `RootCommentId`

### 2. **Enhanced TrackComment Entity**
```csharp
public sealed class TrackComment : Auditable, IEntityCustom
{
    // Basic properties
    public string Id { get; set; }
    public string TrackId { get; set; }
    public string CommenterId { get; set; }
    public string Content { get; set; }

    // Hierarchical properties
    public string? ParentCommentId { get; set; }        // Direct parent
    public string? RootCommentId { get; set; }          // Top-level comment in thread
    public List<string> ThreadPath { get; set; }       // Path from root to this comment
    public int Depth { get; set; }                     // Nesting level (0 = root)
    public long ReplyCount { get; set; }               // Direct replies count
    public long TotalRepliesCount { get; set; }        // Total replies in sub-thread
    
    // Thread management
    public DateTime ThreadUpdatedAt { get; set; }      // Last activity in thread
    public int SortOrder { get; set; }                // Order within same level
}
```

### 3. **Comment Structure Example**
```
Track: "Song Title"
??? Comment A (Root, Depth=0, RootCommentId=null)
?   ??? Reply A1 (Depth=1, RootCommentId=A, ParentCommentId=A)
?   ?   ??? Reply A1a (Depth=2, RootCommentId=A, ParentCommentId=A1)
?   ?   ??? Reply A1b (Depth=2, RootCommentId=A, ParentCommentId=A1)
?   ??? Reply A2 (Depth=1, RootCommentId=A, ParentCommentId=A)
??? Comment B (Root, Depth=0, RootCommentId=null)
?   ??? Reply B1 (Depth=1, RootCommentId=B, ParentCommentId=B)
??? Comment C (Root, Depth=0, RootCommentId=null)
```

## API Methods

### 1. **Create Comment**
```csharp
public async Task CreateCommentAsync(CreateTrackCommentRequest request)
```
- Automatically sets up hierarchy when `ParentCommentId` is provided
- Updates reply counts for all ancestors
- Maintains thread activity timestamps

### 2. **Get Threaded Comments**
```csharp
public async Task<ThreadedCommentsResponse> GetThreadedCommentsAsync(ThreadedCommentsRequest request)
```
- Returns root comments with preview of replies
- Supports pagination and different sort orders
- Includes thread metadata (reply counts, last activity)

### 3. **Get Comment Replies**
```csharp
public async Task<CommentRepliesResponse> GetCommentRepliesAsync(CommentRepliesRequest request)
```
- Returns direct replies to a specific comment
- Supports pagination for large reply sets
- Maintains chronological or custom ordering

### 4. **Get Full Thread**
```csharp
public async Task<List<TrackCommentResponse>> GetCommentThreadAsync(CommentThreadRequest request)
```
- Returns entire conversation thread starting from any comment
- Includes all ancestors and descendants
- Useful for "view conversation" features

## GraphQL Usage Examples

### Query Threaded Comments
```graphql
query GetThreadedComments($trackId: String!, $page: Int!, $pageSize: Int!) {
  getThreadedComments(request: {
    trackId: $trackId
    page: $page
    pageSize: $pageSize
    sortOrder: THREAD_ACTIVITY
  }) {
    threads {
      rootComment {
        id
        content
        depth
        replyCount
        totalRepliesCount
        createdAt
      }
      replies {
        id
        content
        depth
        parentCommentId
        createdAt
      }
      totalReplies
      hasMoreReplies
      lastActivity
    }
    totalThreads
    hasNextPage
  }
}
```

### Query Comment Replies
```graphql
query GetCommentReplies($commentId: String!, $page: Int!) {
  getCommentReplies(request: {
    commentId: $commentId
    page: $page
    pageSize: 10
    sortOrder: CHRONOLOGICAL
  }) {
    replies {
      id
      content
      commenterId
      depth
      replyCount
      createdAt
      isEdited
    }
    totalReplies
    hasNextPage
    parentCommentId
  }
}
```

### Create Nested Reply
```graphql
mutation CreateComment($trackId: String!, $content: String!, $parentCommentId: String) {
  createTrackComment(request: {
    trackId: $trackId
    content: $content
    parentCommentId: $parentCommentId
  })
}
```

## Sorting Options

### 1. **ThreadActivity** (Default)
- Sorts by last activity in the thread
- Brings active conversations to the top
- Best for real-time discussions

### 2. **Chronological**
- Sorts by creation time (oldest first)
- Maintains temporal order
- Good for reading conversations in sequence

### 3. **ReverseChronological**
- Sorts by creation time (newest first)
- Shows latest comments first
- Good for seeing recent activity

### 4. **PopularityBased**
- Sorts by reply count
- Brings popular threads to the top
- Good for highlighting engaging content

## Performance Considerations

### 1. **Efficient Querying**
- Use MongoDB aggregation pipelines for complex queries
- Index on `TrackId`, `RootCommentId`, `Depth`, and `ThreadUpdatedAt`
- Limit depth for UI performance (e.g., max 10 levels)

### 2. **Reply Count Maintenance**
- Atomic updates ensure consistency
- Background jobs can recalculate counts if needed
- Use transactions for multi-document operations

### 3. **Pagination Strategy**
- Root comments paginated separately from replies
- Preview mode shows limited replies per thread
- "Load more" functionality for full reply sets

## Database Indexes

```javascript
// MongoDB indexes for optimal performance
db.trackcomments.createIndex({ "TrackId": 1, "Depth": 1, "ThreadUpdatedAt": -1 })
db.trackcomments.createIndex({ "RootCommentId": 1, "Depth": 1, "CreatedAt": 1 })
db.trackcomments.createIndex({ "ParentCommentId": 1, "CreatedAt": 1 })
db.trackcomments.createIndex({ "CommenterId": 1, "CreatedAt": -1 })
```

## Migration Strategy

### For Existing Flat Comments
```csharp
// Migration script to update existing comments
foreach (var comment in existingComments)
{
    if (comment.ParentCommentId == null)
    {
        // Root comment
        comment.Depth = 0;
        comment.RootCommentId = null;
        comment.ThreadPath = [];
    }
    else
    {
        // Reply - set depth to 1 for backward compatibility
        comment.Depth = 1;
        comment.RootCommentId = FindRootComment(comment.ParentCommentId);
        comment.ThreadPath = [comment.ParentCommentId];
    }
    
    comment.ThreadUpdatedAt = comment.CreatedAt.DateTime;
    // Update reply counts...
}
```

## Frontend Implementation Tips

### 1. **Tree Rendering**
- Use recursive components for nested display
- Implement virtual scrolling for large threads
- Add expand/collapse functionality for deeply nested threads

### 2. **User Experience**
- Show threading lines or indentation
- Highlight the comment being replied to
- Implement "Go to parent" navigation
- Add breadcrumb navigation for deep threads

### 3. **Real-time Updates**
- Use SignalR for live comment updates
- Update reply counts in real-time
- Show typing indicators for active replies

This hierarchical comment system provides a robust foundation for complex comment threading while maintaining performance and usability.