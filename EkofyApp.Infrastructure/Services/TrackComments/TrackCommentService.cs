using EkofyApp.Application.Models.TrackComments;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.TrackComments;

public sealed class TrackCommentService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : ITrackCommentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<TrackComment> GetTrackComments()
    {
        return _unitOfWork.GetCollection<TrackComment>().AsQueryable();
    }

    public async Task CreateCommentAsync(CreateTrackCommentRequest request)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value 
            ?? throw new UnauthorizedCustomException("Your session is limit");

        // Verify track exists
        bool isTrackExisted = await _unitOfWork.GetCollection<Track>()
            .Find(t => t.Id == request.TrackId)
            .AnyAsync() ? true : throw new NotFoundCustomException("Track not found");

        TrackComment comment = new()
        {
            TrackId = request.TrackId,
            CommenterId = userId,
            Content = request.Content,
            CreatedAt = HelperMethod.GetUtcPlus7TimeOffset(),
            UpdatedAt = HelperMethod.GetUtcPlus7TimeOffset(),
            ThreadUpdatedAt = HelperMethod.GetUtcPlus7TimeOffset()
        };

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
            await _unitOfWork.GetCollection<TrackComment>().InsertOneAsync(session, comment);

            // Update parent comment reply counts if this is a reply
            if (!string.IsNullOrEmpty(comment.ParentCommentId))
            {
                await UpdateParentReplyCounts(session, comment.ParentCommentId, comment.RootCommentId!, 1);
            }
        });
    }

    public async Task UpdateCommentAsync(UpdateTrackCommentRequest request)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value 
            ?? throw new UnauthorizedCustomException("Your session is limit");

        TrackComment? comment = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.Id == request.CommentId && !c.IsDeleted)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Comment not found");
        
        if (comment.CommenterId != userId)
        {
            throw new UnauthorizedCustomException("You can only edit your own comments");
        }

        UpdateDefinition<TrackComment> update = Builders<TrackComment>.Update
            .Set(c => c.Content, request.Content)
            .Set(c => c.IsEdited, true)
            .Set(c => c.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        UpdateResult result = await _unitOfWork.GetCollection<TrackComment>()
            .UpdateOneAsync(c => c.Id == request.CommentId, update);

        if (result.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Failed to update comment");
        }
    }

    public async Task DeleteCommentAsync(DeleteTrackCommentRequest request)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value 
            ?? throw new UnauthorizedCustomException("Your session is limit");

        TrackComment? comment = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.Id == request.CommentId && !c.IsDeleted)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Comment not found");
        
        if (comment.CommenterId != userId)
        {
            throw new UnauthorizedCustomException("You can only delete your own comments");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Mark comment as deleted
            UpdateDefinition<TrackComment> update = Builders<TrackComment>.Update
                .Set(c => c.IsDeleted, true)
                .Set(c => c.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

            await _unitOfWork.GetCollection<TrackComment>()
                .UpdateOneAsync(session, c => c.Id == request.CommentId, update);

            // Update parent reply counts
            if (!string.IsNullOrEmpty(comment.ParentCommentId))
            {
                await UpdateParentReplyCounts(session, comment.ParentCommentId, comment.RootCommentId!, -1);
            }
        });
    }

    public async Task<ThreadedCommentsResponse> GetThreadedCommentsAsync(ThreadedCommentsRequest request)
    {
        // Get total thread count
        var totalThreads = await _unitOfWork.GetCollection<TrackComment>()
            .CountDocumentsAsync(c => c.TrackId == request.TrackId && c.Depth == 0 && !c.IsDeleted);

        // Build sort definition based on request
        var sortDefinition = GetSortDefinition(request.SortOrder);

        // Get root comments
        var rootComments = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.TrackId == request.TrackId && c.Depth == 0 && !c.IsDeleted)
            .Sort(sortDefinition)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync();

        var threads = new List<CommentThread>();

        foreach (var rootComment in rootComments)
        {
            // Get a few top-level replies for preview (e.g., first 3)
            var previewReplies = await _unitOfWork.GetCollection<TrackComment>()
                .Find(c => c.ParentCommentId == rootComment.Id && !c.IsDeleted)
                .SortBy(c => c.CreatedAt)
                .Limit(3)
                .ToListAsync();

            var thread = new CommentThread
            {
                RootComment = MapToResponse(rootComment),
                Replies = previewReplies.Select(MapToResponse).ToList(),
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
            HasNextPage = (request.Page * request.PageSize) < totalThreads
        };
    }

    public async Task<CommentRepliesResponse> GetCommentRepliesAsync(CommentRepliesRequest request)
    {
        var parentComment = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.Id == request.CommentId && !c.IsDeleted)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Parent comment not found");

        var totalReplies = await _unitOfWork.GetCollection<TrackComment>()
            .CountDocumentsAsync(c => c.ParentCommentId == request.CommentId && !c.IsDeleted);

        var sortDefinition = GetSortDefinition(request.SortOrder);

        var replies = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.ParentCommentId == request.CommentId && !c.IsDeleted)
            .Sort(sortDefinition)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync();

        return new CommentRepliesResponse
        {
            Replies = replies.Select(MapToResponse).ToList(),
            TotalReplies = (int)totalReplies,
            Page = request.Page,
            PageSize = request.PageSize,
            HasNextPage = (request.Page * request.PageSize) < totalReplies,
            ParentCommentId = request.CommentId
        };
    }

    public async Task<List<TrackCommentResponse>> GetCommentThreadAsync(CommentThreadRequest request)
    {
        var comment = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.Id == request.CommentId)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Comment not found");

        var rootId = comment.RootCommentId ?? comment.Id;

        var filter = Builders<TrackComment>.Filter.And(
            Builders<TrackComment>.Filter.Or(
                Builders<TrackComment>.Filter.Eq(c => c.Id, rootId),
                Builders<TrackComment>.Filter.Eq(c => c.RootCommentId, rootId)
            )
        );

        if (!request.IncludeDeleted)
        {
            filter = Builders<TrackComment>.Filter.And(filter, 
                Builders<TrackComment>.Filter.Eq(c => c.IsDeleted, false));
        }

        var threadComments = await _unitOfWork.GetCollection<TrackComment>()
            .Find(filter)
            .SortBy(c => c.Depth)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync();

        return threadComments.Select(MapToResponse).ToList();
    }

    public async Task<int> GetCommentDepthAsync(string commentId)
    {
        var comment = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.Id == commentId)
            .Project(c => new { c.Depth })
            .FirstOrDefaultAsync();

        return comment?.Depth ?? 0;
    }

    public async Task<bool> IsCommentInThreadAsync(string commentId, string threadRootId)
    {
        var comment = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.Id == commentId)
            .Project(c => new { c.RootCommentId, c.Id })
            .FirstOrDefaultAsync();

        if (comment == null) return false;

        var rootId = comment.RootCommentId ?? comment.Id;
        return rootId == threadRootId;
    }

    // Helper method to setup reply hierarchy
    private async Task SetupReplyHierarchy(TrackComment comment, string parentCommentId)
    {
        TrackComment? parentComment = await _unitOfWork.GetCollection<TrackComment>()
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
    private static void SetupRootComment(TrackComment comment)
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
        var parentUpdate = Builders<TrackComment>.Update
            .Inc(c => c.ReplyCount, increment)
            .Set(c => c.ThreadUpdatedAt, HelperMethod.GetUtcPlus7TimeOffset().DateTime);

        await _unitOfWork.GetCollection<TrackComment>()
            .UpdateOneAsync(session, c => c.Id == parentCommentId, parentUpdate);

        // Update all ancestors' total reply counts
        var ancestorUpdate = Builders<TrackComment>.Update
            .Inc(c => c.TotalRepliesCount, increment)
            .Set(c => c.ThreadUpdatedAt, HelperMethod.GetUtcPlus7TimeOffset().DateTime);

        // Update root comment total count
        await _unitOfWork.GetCollection<TrackComment>()
            .UpdateOneAsync(session, c => c.Id == rootCommentId, ancestorUpdate);

        // Update all comments in the thread path (ancestors)
        TrackComment? parentComment = await _unitOfWork.GetCollection<TrackComment>()
            .Find(c => c.Id == parentCommentId)
            .FirstOrDefaultAsync();

        if (parentComment?.ThreadPath?.Count > 0)
        {
            var ancestorIds = parentComment.ThreadPath;
            await _unitOfWork.GetCollection<TrackComment>()
                .UpdateManyAsync(session, 
                    c => ancestorIds.Contains(c.Id), 
                    ancestorUpdate);
        }
    }

    // Helper method to map entity to response
    private static TrackCommentResponse MapToResponse(TrackComment comment)
    {
        return new TrackCommentResponse
        {
            Id = comment.Id,
            TrackId = comment.TrackId,
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
            CreatedAt = comment.CreatedAt.DateTime,
            UpdatedAt = comment.UpdatedAt?.DateTime,
            ThreadUpdatedAt = comment.ThreadUpdatedAt
        };
    }

    // Helper method to get sort definition based on sort order
    private static SortDefinition<TrackComment> GetSortDefinition(CommentSortOrder sortOrder)
    {
        return sortOrder switch
        {
            CommentSortOrder.ThreadActivity => Builders<TrackComment>.Sort
                .Descending(c => c.ThreadUpdatedAt)
                .Descending(c => c.CreatedAt),
            CommentSortOrder.PopularityBased => Builders<TrackComment>.Sort
                .Descending(c => c.TotalRepliesCount)
                .Descending(c => c.CreatedAt),
            CommentSortOrder.ReverseChronological => Builders<TrackComment>.Sort
                .Descending(c => c.CreatedAt),
            CommentSortOrder.Chronological => Builders<TrackComment>.Sort
                .Ascending(c => c.CreatedAt),
            _ => Builders<TrackComment>.Sort
                .Descending(c => c.ThreadUpdatedAt)
                .Descending(c => c.CreatedAt)
        };
    }
}