using EkofyApp.Application.Models.Comments;
using EkofyApp.Application.Models.Notifications;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Serilog;

namespace EkofyApp.Infrastructure.Services.Comments;

public sealed class CommentService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IHubContext<NotificationHub> hubContext, INotificationService notificationService) : ICommentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
    private readonly INotificationService _notificationService = notificationService;

    public IQueryable<Comment> GetTrackComments()
    {
        return _unitOfWork.GetCollection<Comment>().AsQueryable().Where(x => x.CommentType == CommentType.Track);
    }

    public IQueryable<Comment> GetRequestHubComments()
    {
        return _unitOfWork.GetCollection<Comment>().AsQueryable().Where(x => x.CommentType == CommentType.Request);
    }

    public async Task CreateCommentAsync(CreateCommentRequest request)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        // Verify target exists based on comment type
        await VerifyTargetExists(request.TargetId, request.CommentType);

        Comment comment = new()
        {
            TargetId = request.TargetId,
            CommentType = request.CommentType,
            CommenterId = userId,
            Content = request.Content,
            CreatedAt = HelperMethod.GetUtcPlus7TimeOffset(),
            UpdatedAt = HelperMethod.GetUtcPlus7TimeOffset(),
            ThreadUpdatedAt = HelperMethod.GetUtcPlus7TimeOffset()
        };

        // Cập nhật track daily metrics
        if (request.CommentType == CommentType.Track)
        {
            DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();
            DateTimeOffset startOfDay = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
            DateTimeOffset endOfDay = startOfDay.AddDays(1);

            await _unitOfWork.GetCollection<TrackDailyMetric>().UpdateOneAsync(
                x => x.TrackId == request.TargetId &&
                     x.CreatedAt >= startOfDay &&
                     x.CreatedAt < endOfDay,
                Builders<TrackDailyMetric>.Update
                    .Inc(x => x.CommentCount, 1)
                    .Set(x => x.UpdatedAt, now)
                    .SetOnInsert(x => x.CreatedAt, now)
                    .SetOnInsert(x => x.StreamCount, 0)
                    .SetOnInsert(x => x.DownloadCount, 0)
                    .SetOnInsert(x => x.FavoriteCount, 0),
                new UpdateOptions { IsUpsert = true }
            );
        }

        // Handle hierarchical structure
        if (!string.IsNullOrEmpty(request.ParentCommentId))
        {
            await SetupReplyHierarchy(comment, request.ParentCommentId);
        }
        else
        {
            // This is a root comment
            SetupRootComment(comment);
        }

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Insert the new comment
            await _unitOfWork.GetCollection<Comment>().InsertOneAsync(session, comment);

            // Update parent comment reply counts if this is a reply
            if (!string.IsNullOrEmpty(comment.ParentCommentId))
            {
                await UpdateParentReplyCounts(session, comment.ParentCommentId, comment.RootCommentId!, 1);
            }
        });

        // Send notifications based on comment type
        if (string.IsNullOrEmpty(request.ParentCommentId))
        {
            // This is a root comment - notify the content creator
            switch (request.CommentType)
            {
                case CommentType.Track:
                    await SendTrackCommentNotificationAsync(request.TargetId, userId);
                    break;
                case CommentType.Request:
                    await SendRequestCommentNotificationAsync(request.TargetId, userId);
                    break;
            }
        }
        else
        {
            // This is a reply comment - notify the parent comment author
            await SendReplyNotificationAsync(request.ParentCommentId, userId);
        }
    }

    public async Task UpdateCommentAsync(UpdateTrackCommentRequest request)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        Comment? comment = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.Id == request.CommentId && !c.IsDeleted)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Comment not found");

        if (comment.CommenterId != userId)
        {
            throw new UnauthorizedCustomException("You can only edit your own comments");
        }

        UpdateDefinition<Comment> update = Builders<Comment>.Update
            .Set(c => c.Content, request.Content)
            .Set(c => c.IsEdited, true)
            .Set(c => c.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        UpdateResult result = await _unitOfWork.GetCollection<Comment>()
            .UpdateOneAsync(c => c.Id == request.CommentId, update);

        if (result.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update comment");
        }
    }

    public async Task DeleteCommentAsync(DeleteCommentRequest request)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        Comment? comment = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.Id == request.CommentId && !c.IsDeleted)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Comment not found");

        if (comment.CommenterId != userId)
        {
            throw new UnauthorizedCustomException("You can only delete your own comments");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Get all reply IDs to be deleted (recursively)
            var replyIds = await GetAllReplyIds(comment.Id);
            var totalRepliesDeleted = replyIds.Count;

            // Hard delete all replies first
            if (replyIds.Count > 0)
            {
                await _unitOfWork.GetCollection<Comment>()
                    .DeleteManyAsync(session, c => replyIds.Contains(c.Id));
            }

            // Hard delete the main comment
            await _unitOfWork.GetCollection<Comment>()
                .DeleteOneAsync(session, c => c.Id == request.CommentId);

            // Update parent reply counts if this comment had a parent
            if (!string.IsNullOrEmpty(comment.ParentCommentId))
            {
                // Decrement by 1 (for the deleted comment) + total replies deleted
                var decrementAmount = -(1 + totalRepliesDeleted);
                await UpdateParentReplyCounts(session, comment.ParentCommentId, comment.RootCommentId!, decrementAmount);
            }

            DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();
            DateTimeOffset startOfDay = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
            DateTimeOffset endOfDay = startOfDay.AddDays(1);

            string trackId = await _unitOfWork.GetCollection<Comment>()
                .Find(c => c.Id == request.CommentId)
                .Project(c => c.TargetId)
                .FirstOrDefaultAsync() ?? string.Empty;

            if(string.IsNullOrEmpty(trackId))
            {
                return;
            }

            await unitOfWork.GetCollection<TrackDailyMetric>().UpdateOneAsync(session,
                x => x.TrackId == trackId &&
                     x.CreatedAt >= startOfDay &&
                     x.CreatedAt < endOfDay,
                Builders<TrackDailyMetric>.Update
                    .Inc(x => x.CommentCount, -1)
                    .Set(x => x.UpdatedAt, now)
                    .SetOnInsert(x => x.CreatedAt, now)
                    .SetOnInsert(x => x.StreamCount, 0)
                    .SetOnInsert(x => x.DownloadCount, 0)
                    .SetOnInsert(x => x.FavoriteCount, 0),
                new UpdateOptions { IsUpsert = true }
            );
        });
    }

    public async Task<ThreadedCommentsResponse> GetThreadedCommentsAsync(ThreadedCommentsRequest request)
    {
        // Get total thread count
        long totalThreads = await _unitOfWork.GetCollection<Comment>()
            .CountDocumentsAsync(c => c.TargetId == request.TargetId && c.CommentType == request.CommentType && c.Depth == 0 && !c.IsDeleted && c.IsVisible);

        // Build sort definition based on request
        SortDefinition<Comment> sortDefinition = GetSortDefinition(request.SortOrder);

        // Get root comments
        List<Comment> rootComments = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.TargetId == request.TargetId && c.CommentType == request.CommentType && c.Depth == 0 && !c.IsDeleted && c.IsVisible)
            .Sort(sortDefinition)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync();

        List<CommentThread> threads = [];

        foreach (Comment rootComment in rootComments)
        {
            // Get a few top-level replies for preview (e.g., first 3)
            List<Comment> previewReplies = await _unitOfWork.GetCollection<Comment>()
                .Find(c => c.ParentCommentId == rootComment.Id && !c.IsDeleted && c.IsVisible)
                .SortBy(c => c.CreatedAt)
                //.Limit(3)
                .ToListAsync();

            // Map root comment and replies with user information
            CommentResponse rootCommentResponse = await MapToResponseAsync(rootComment);
            List<CommentResponse> repliesWithUserInfo = [];

            foreach (Comment reply in previewReplies)
            {
                repliesWithUserInfo.Add(await MapToResponseAsync(reply));
            }

            CommentThread thread = new()
            {
                RootComment = rootCommentResponse,
                Replies = repliesWithUserInfo,
                TotalReplies = (int)rootComment.TotalRepliesCount,
                HasMoreReplies = rootComment.TotalRepliesCount > 3,
                LastActivity = rootComment.ThreadUpdatedAt
            };

            threads.Add(thread);
        }

        return new ThreadedCommentsResponse
        {
            Threads = threads,
            TotalThreads = (int)totalThreads,
            Page = request.Page,
            PageSize = request.PageSize,
            HasNextPage = request.Page * request.PageSize < totalThreads
        };
    }

    public async Task<CommentRepliesResponse> GetCommentRepliesAsync(CommentRepliesRequest request)
    {
        var parentComment = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.Id == request.CommentId && !c.IsDeleted)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Parent comment not found");

        var totalReplies = await _unitOfWork.GetCollection<Comment>()
            .CountDocumentsAsync(c => c.ParentCommentId == request.CommentId && !c.IsDeleted);

        var sortDefinition = GetSortDefinition(request.SortOrder);

        List<Comment> replies = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.ParentCommentId == request.CommentId && !c.IsDeleted)
            .Sort(sortDefinition)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync();

        // Map replies with user information
        var repliesWithUserInfo = new List<CommentResponse>();
        foreach (var reply in replies)
        {
            repliesWithUserInfo.Add(await MapToResponseAsync(reply));
        }

        return new CommentRepliesResponse
        {
            Replies = repliesWithUserInfo,
            TotalReplies = (int)totalReplies,
            Page = request.Page,
            PageSize = request.PageSize,
            HasNextPage = request.Page * request.PageSize < totalReplies,
            ParentCommentId = request.CommentId
        };
    }

    public async Task<List<CommentResponse>> GetCommentThreadAsync(CommentThreadRequest request)
    {
        Comment comment = await _unitOfWork.GetCollection<Comment>()
        .Find(c => c.Id == request.CommentId)
           .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Comment not found");

        string rootId = comment.RootCommentId ?? comment.Id;

        FilterDefinition<Comment> filter = Builders<Comment>.Filter.And(
                Builders<Comment>.Filter.Or(
                Builders<Comment>.Filter.Eq(c => c.Id, rootId),
                Builders<Comment>.Filter.Eq(c => c.RootCommentId, rootId))
        );

        if (!request.IncludeDeleted)
        {
            filter = Builders<Comment>.Filter.And(filter,
              Builders<Comment>.Filter.Eq(c => c.IsDeleted, false));
        }

        List<Comment> threadComments = await _unitOfWork.GetCollection<Comment>()
            .Find(filter)
            .SortBy(c => c.Depth)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync();

        // Map comments with user information
        List<CommentResponse> commentsWithUserInfo = [];
        foreach (Comment threadComment in threadComments)
        {
            commentsWithUserInfo.Add(await MapToResponseAsync(threadComment));
        }

        return commentsWithUserInfo;
    }

    public async Task<int> GetCommentDepthAsync(string commentId)
    {
        var comment = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.Id == commentId)
            .Project(c => new { c.Depth })
            .FirstOrDefaultAsync();

        return comment?.Depth ?? 0;
    }

    public async Task<bool> IsCommentInThreadAsync(string commentId, string threadRootId)
    {
        var comment = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.Id == commentId)
            .Project(c => new { c.RootCommentId, c.Id })
            .FirstOrDefaultAsync();

        if (comment == null)
        {
            return false;
        }

        string rootId = comment.RootCommentId ?? comment.Id;
        return rootId == threadRootId;
    }

    // Helper method to verify target exists based on comment type
    private async Task VerifyTargetExists(string targetId, CommentType commentType)
    {
        bool exists = commentType switch
        {
            CommentType.Track => await _unitOfWork.GetCollection<Track>()
                .Find(t => t.Id == targetId)
                .AnyAsync(),
            CommentType.Playlist => await _unitOfWork.GetCollection<Playlist>()
                .Find(p => p.Id == targetId)
                .AnyAsync(),
            CommentType.Album => await _unitOfWork.GetCollection<Album>()
                .Find(a => a.Id == targetId)
                .AnyAsync(),
            CommentType.Request => await _unitOfWork.GetCollection<Request>()
                .Find(r => r.Id == targetId)
                .AnyAsync(),
            _ => false
        };

        if (!exists)
        {
            throw new NotFoundCustomException($"{commentType} not found");
        }
    }

    // Helper method to setup reply hierarchy
    private async Task SetupReplyHierarchy(Comment comment, string parentCommentId)
    {
        Comment? parentComment = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.Id == parentCommentId && !c.IsDeleted)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Parent comment not found");

        comment.ParentCommentId = parentCommentId;
        comment.RootCommentId = parentComment.RootCommentId ?? parentComment.Id;
        comment.Depth = parentComment.Depth + 1;

        // Build thread path: parent's path + parent's ID
        comment.ThreadPath = [.. parentComment.ThreadPath, parentCommentId];

        // Set sort order (could be based on creation time or other criteria)
        comment.SortOrder = 0; // New replies start at 0, can be adjusted based on business logic
    }

    // Helper method to setup root comment
    private static void SetupRootComment(Comment comment)
    {
        comment.ParentCommentId = null;
        comment.RootCommentId = null; // Will be set to its own ID after insertion if needed
        comment.Depth = 0;
        comment.ThreadPath = [];
        comment.SortOrder = 0;
    }

    // Helper method to update parent reply counts recursively
    private async Task UpdateParentReplyCounts(IClientSessionHandle session, string parentCommentId, string rootCommentId, int increment)
    {
        // Update direct parent reply count
        UpdateDefinition<Comment> parentUpdate = Builders<Comment>.Update
            .Inc(c => c.ReplyCount, increment)
            .Set(c => c.ThreadUpdatedAt, HelperMethod.GetUtcPlus7TimeOffset().DateTime);

        await _unitOfWork.GetCollection<Comment>()
            .UpdateOneAsync(session, c => c.Id == parentCommentId, parentUpdate);

        // Update all ancestors' total reply counts
        UpdateDefinition<Comment> ancestorUpdate = Builders<Comment>.Update
            .Inc(c => c.TotalRepliesCount, increment)
            .Set(c => c.ThreadUpdatedAt, HelperMethod.GetUtcPlus7TimeOffset().DateTime);

        // Update root comment total count
        await _unitOfWork.GetCollection<Comment>()
            .UpdateOneAsync(session, c => c.Id == rootCommentId, ancestorUpdate);

        // Update all comments in the thread path (ancestors)
        Comment? parentComment = await _unitOfWork.GetCollection<Comment>()
            .Find(c => c.Id == parentCommentId)
            .FirstOrDefaultAsync();

        if (parentComment?.ThreadPath?.Count > 0)
        {
            List<string> ancestorIds = parentComment.ThreadPath;
            await _unitOfWork.GetCollection<Comment>()
                .UpdateManyAsync(session,
                    c => ancestorIds.Contains(c.Id),
                    ancestorUpdate);
        }
    }

    // Helper method to map entity to response with user information
    private async Task<CommentResponse> MapToResponseAsync(Comment comment)
    {
        CommenterInfo commenterInfo = await GetCommenterInfoAsync(comment.CommenterId);

        return new CommentResponse
        {
            Id = comment.Id,
            TargetId = comment.TargetId,
            CommentType = comment.CommentType,
            CommenterId = comment.CommenterId,
            Content = comment.Content,
            ParentCommentId = comment.ParentCommentId,
            RootCommentId = comment.RootCommentId,
            ThreadPath = comment.ThreadPath,
            Depth = comment.Depth,
            ReplyCount = comment.ReplyCount,
            TotalRepliesCount = comment.TotalRepliesCount,
            IsEdited = comment.IsEdited,
            IsDeleted = comment.IsDeleted,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            ThreadUpdatedAt = comment.ThreadUpdatedAt,
            Commenter = commenterInfo
        };
    }

    // Helper method to get commenter information including User, Listener, and Artist data
    private async Task<CommenterInfo> GetCommenterInfoAsync(string userId)
    {
        // Get user information
        var user = await _unitOfWork.GetCollection<User>()
        .Find(u => u.Id == userId)
      .Project(u => new { u.Id, u.FullName, u.Email, u.Role })
     .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new NotFoundCustomException($"User with ID {userId} not found");
        }

        // Get listener information if exists
        ListenerInfo? listenerInfo = null;
        var listener = await _unitOfWork.GetCollection<Listener>()
                  .Find(l => l.UserId == userId)
                .Project(l => new { l.Id, l.DisplayName, l.AvatarImage, l.IsVerified, l.FollowerCount })
             .FirstOrDefaultAsync();

        if (listener != null)
        {
            listenerInfo = new ListenerInfo
            {
                Id = listener.Id,
                DisplayName = listener.DisplayName,
                AvatarImage = listener.AvatarImage,
                IsVerified = listener.IsVerified,
                FollowerCount = listener.FollowerCount
            };
        }

        // Get artist information if exists
        ArtistInfo? artistInfo = null;
        var artist = await _unitOfWork.GetCollection<Artist>()
            .Find(a => a.UserId == userId)
         .Project(a => new { a.Id, a.StageName, a.AvatarImage, a.IsVerified, a.FollowerCount, a.Popularity })
              .FirstOrDefaultAsync();

        if (artist != null)
        {
            artistInfo = new ArtistInfo
            {
                Id = artist.Id,
                StageName = artist.StageName,
                AvatarImage = artist.AvatarImage,
                IsVerified = artist.IsVerified,
                FollowerCount = artist.FollowerCount,
                Popularity = artist.Popularity
            };
        }

        return new CommenterInfo
        {
            UserId = user.Id,
            FullName = user.FullName,
            Avatar = listener?.AvatarImage ?? artist?.AvatarImage ?? string.Empty,
            Email = user.Email,
            Role = user.Role,
            IsVerified = listener?.IsVerified ?? artist?.IsVerified ?? false,
            Listener = listenerInfo,
            Artist = artistInfo
        };
    }

    // Helper method to map entity to response (kept for backward compatibility)
    private static CommentResponse MapToResponse(Comment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            TargetId = comment.TargetId,
            CommentType = comment.CommentType,
            CommenterId = comment.CommenterId,
            Content = comment.Content,
            ParentCommentId = comment.ParentCommentId,
            RootCommentId = comment.RootCommentId,
            ThreadPath = comment.ThreadPath,
            Depth = comment.Depth,
            ReplyCount = comment.ReplyCount,
            TotalRepliesCount = comment.TotalRepliesCount,
            IsEdited = comment.IsEdited,
            IsDeleted = comment.IsDeleted,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            ThreadUpdatedAt = comment.ThreadUpdatedAt,
            Commenter = new CommenterInfo
            {
                UserId = comment.CommenterId,
                FullName = "Unknown User",
                Email = "unknown@example.com",
                Role = UserRole.Listener,
                IsVerified = false,
                Listener = null,
                Artist = null
            }
        };
    }

    // Helper method to get sort definition based on sort order
    private static SortDefinition<Comment> GetSortDefinition(CommentSortOrder sortOrder)
    {
        return sortOrder switch
        {
            CommentSortOrder.ThreadActivity => Builders<Comment>.Sort
                .Descending(c => c.ThreadUpdatedAt)
                .Descending(c => c.CreatedAt),
            CommentSortOrder.PopularityBased => Builders<Comment>.Sort
                .Descending(c => c.TotalRepliesCount)
                .Descending(c => c.CreatedAt),
            CommentSortOrder.ReverseChronological => Builders<Comment>.Sort
                .Descending(c => c.CreatedAt),
            CommentSortOrder.Chronological => Builders<Comment>.Sort
                .Ascending(c => c.CreatedAt),
            _ => Builders<Comment>.Sort
                .Descending(c => c.ThreadUpdatedAt)
                .Descending(c => c.CreatedAt)
        };
    }

    // Helper method to get all reply IDs recursively
    private async Task<List<string>> GetAllReplyIds(string commentId)
    {
        var allReplyIds = new List<string>();
        var queue = new Queue<string>();
        queue.Enqueue(commentId);

        while (queue.Count > 0)
        {
            var currentCommentId = queue.Dequeue();

            // Find direct replies to current comment
            var directReplies = await _unitOfWork.GetCollection<Comment>()
                          .Find(c => c.ParentCommentId == currentCommentId && !c.IsDeleted)
                  .Project(c => c.Id)
              .ToListAsync();

            foreach (var replyId in directReplies)
            {
                allReplyIds.Add(replyId);
                queue.Enqueue(replyId); // Add to queue to find its replies
            }
        }

        return allReplyIds;
    }

    // Helper method to send notification to track creator for root comment
    private async Task SendTrackCommentNotificationAsync(string trackId, string commenterId)
    {
        try
        {
            // Get track information to find the creator
            var track = await _unitOfWork.GetCollection<Track>()
                .Find(t => t.Id == trackId)
                .Project(t => new { t.Id, t.Name, t.CreatedBy })
                .FirstOrDefaultAsync();

            if (track != null && track.CreatedBy != commenterId)
            {
                // Get commenter info
                var commenterInfo = await GetCommenterInfoAsync(commenterId);
                string commenterName = commenterInfo.Artist?.StageName ?? 
                                     commenterInfo.Listener?.DisplayName ?? 
                                     commenterInfo.FullName;

                string content = HelperMethod.BuildContentNotification(NotificationActionType.Comment, NotificationRelatedType.Track, track.Name, commenterName);

                await _unitOfWork.GetCollection<Notification>()
                    .InsertOneAsync(new Notification
                    {
                        ActorId = commenterId,
                        TargetId = track.CreatedBy!,
                        Content = content,
                        Action = NotificationActionType.Comment,
                        RelatedId = trackId,
                        RelatedType = NotificationRelatedType.Track,
                        Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/track/{track.Id}"
                    });

                await _hubContext.Clients.User(track.CreatedBy!).SendAsync("ReceiveNotification", new NotificationResponse
                {
                    Content = content,
                    Avatar = commenterInfo.Listener?.AvatarImage ?? commenterInfo.Artist?.AvatarImage ?? string.Empty,
                });
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the comment creation
            Log.Error($"Error sending track comment notification: {ex}");
        }
    }

    // Helper method to send notification to user being replied to
    private async Task SendReplyNotificationAsync(string parentCommentId, string replierId)
    {
        try
        {
            // Get parent comment information
            var parentComment = await _unitOfWork.GetCollection<Comment>()
                .Find(c => c.Id == parentCommentId && !c.IsDeleted)
                .Project(c => new { c.CommenterId, c.TargetId, c.CommentType })
                .FirstOrDefaultAsync();

            if (parentComment != null && parentComment.CommenterId != replierId)
            {
                // Get replier info
                var replierInfo = await GetCommenterInfoAsync(replierId);
                string replierName = replierInfo.Artist?.StageName ?? 
                                   replierInfo.Listener?.DisplayName ?? 
                                   replierInfo.FullName;

                // Get target name (track, playlist, etc.)
                string targetName = await GetTargetNameAsync(parentComment.TargetId, parentComment.CommentType);
                
                string content = HelperMethod.BuildContentNotification(NotificationActionType.Comment, NotificationRelatedType.Comment, null, replierName);
                
                string url = parentComment.CommentType switch
                {
                    CommentType.Track => $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/track/{parentComment.TargetId}",
                    CommentType.Playlist => $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/playlist/{parentComment.TargetId}",
                    CommentType.Album => $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/album/{parentComment.TargetId}",
                    CommentType.Request => $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/request-hub/{parentComment.TargetId}",
                    _ => throw new BadRequestCustomException("Invalid comment type for notification")
                };

                await _unitOfWork.GetCollection<Notification>()
                    .InsertOneAsync(new Notification
                    {
                        ActorId = replierId,
                        TargetId = parentComment.CommenterId,
                        Content = content,
                        Action = NotificationActionType.Reply,
                        RelatedId = parentComment.TargetId,
                        RelatedType = parentComment.CommentType switch
                        {
                            CommentType.Track => NotificationRelatedType.Track,
                            CommentType.Playlist => NotificationRelatedType.Playlist,
                            CommentType.Album => NotificationRelatedType.Album,
                            CommentType.Request => NotificationRelatedType.Request,
                            _ => NotificationRelatedType.Comment
                        },
                        Url = url,
                    });

                await _hubContext.Clients.User(parentComment.CommenterId).SendAsync("ReceiveNotification", new NotificationResponse
                {
                    Content = content,
                    Avatar = replierInfo.Listener?.AvatarImage ?? replierInfo.Artist?.AvatarImage ?? string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the comment creation
            Log.Error($"Error sending reply notification: {ex}");
        }
    }

    // Helper method to send notification to request creator for root comment
    private async Task SendRequestCommentNotificationAsync(string requestId, string commenterId)
    {
        try
        {
            // Get request information to find the creator
            var request = await _unitOfWork.GetCollection<Request>()
                .Find(r => r.Id == requestId)
                .Project(r => new { r.Id, r.Title, r.RequestUserId })
                .FirstOrDefaultAsync();

            if (request != null && request.RequestUserId != commenterId)
            {
                // Get commenter info
                var commenterInfo = await GetCommenterInfoAsync(commenterId);
                string commenterName = commenterInfo.Artist?.StageName ?? 
                                     commenterInfo.Listener?.DisplayName ?? 
                                     commenterInfo.FullName;

                string content = HelperMethod.BuildContentNotification(NotificationActionType.Comment, NotificationRelatedType.Request, request.Title, commenterName);

                await _unitOfWork.GetCollection<Notification>()
                    .InsertOneAsync(new Notification
                    {
                        ActorId = commenterId,
                        TargetId = request.RequestUserId,
                        Content = content,
                        Action = NotificationActionType.Comment,
                        RelatedId = requestId,
                        RelatedType = NotificationRelatedType.Request,
                        Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/request-hub/{request.Id}"
                    });

                await _hubContext.Clients.User(request.RequestUserId).SendAsync("ReceiveNotification", new NotificationResponse
                {
                    Content = content,
                    Avatar = commenterInfo.Listener?.AvatarImage ?? commenterInfo.Artist?.AvatarImage ?? string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the comment creation
            Log.Error($"Error sending request comment notification: {ex}");
        }
    }

    // Helper method to get target name based on comment type
    private async Task<string> GetTargetNameAsync(string targetId, CommentType commentType)
    {
        try
        {
            return commentType switch
            {
                CommentType.Track => await _unitOfWork.GetCollection<Track>()
                    .Find(t => t.Id == targetId)
                    .Project(t => t.Name)
                    .FirstOrDefaultAsync() ?? "Unknown Track",
                    
                CommentType.Playlist => await _unitOfWork.GetCollection<Playlist>()
                    .Find(p => p.Id == targetId)
                    .Project(p => p.Name)
                    .FirstOrDefaultAsync() ?? "Unknown Playlist",
                    
                CommentType.Album => await _unitOfWork.GetCollection<Album>()
                    .Find(a => a.Id == targetId)
                    .Project(a => a.Name)
                    .FirstOrDefaultAsync() ?? "Unknown Album",
                    
                CommentType.Request => await _unitOfWork.GetCollection<Request>()
                    .Find(r => r.Id == targetId)
                    .Project(r => r.Title)
                    .FirstOrDefaultAsync() ?? "Unknown Request",
                    
                _ => "Unknown Content"
            };
        }
        catch
        {
            return "Unknown Content";
        }
    }
}