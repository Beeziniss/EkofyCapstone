using EkofyApp.Application.Models.Comments;
using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Comments;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class CommentQuery(ICommentService commentService)
{
    private readonly ICommentService _commentService = commentService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Comment>]
    public IQueryable<Comment> GetTrackComments()
    {
        return _commentService.GetTrackComments();
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Comment>]
    public IQueryable<Comment> GetRequestHubComments()
    {
        return _commentService.GetRequestHubComments();
    }

    #region Track Comments
    // New hierarchical comment queries
    [UseProjection]
    public async Task<ThreadedCommentsResponse> GetThreadedCommentsAsync(ThreadedCommentsRequest request)
    {
        return await _commentService.GetThreadedCommentsAsync(request);
    }

    [UseProjection]
    public async Task<CommentRepliesResponse> GetCommentRepliesAsync(CommentRepliesRequest request)
    {
        return await _commentService.GetCommentRepliesAsync(request);
    }

    [UseProjection]
    public async Task<List<CommentResponse>> GetCommentThreadAsync(CommentThreadRequest request)
    {
        return await _commentService.GetCommentThreadAsync(request);
    }

    public async Task<int> GetCommentDepthAsync(string commentId)
    {
        return await _commentService.GetCommentDepthAsync(commentId);
    }

    public async Task<bool> IsCommentInThreadAsync(string commentId, string threadRootId)
    {
        return await _commentService.IsCommentInThreadAsync(commentId, threadRootId);
    }
    #endregion
}
