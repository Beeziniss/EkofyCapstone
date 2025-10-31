using EkofyApp.Application.Models.TrackComments;
using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Comments;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class CommentQuery(ICommentService trackCommentService)
{
    private readonly ICommentService _trackCommentService = trackCommentService;

    #region Track Comments
    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Comment>]
    public IQueryable<Comment> GetTrackComments()
    {
        return _trackCommentService.GetTrackComments();
    }

    // New hierarchical comment queries
    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseProjection]
    public async Task<ThreadedCommentsResponse> GetThreadedCommentsAsync(ThreadedCommentsRequest request)
    {
        return await _trackCommentService.GetThreadedCommentsAsync(request);
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseProjection]
    public async Task<CommentRepliesResponse> GetCommentRepliesAsync(CommentRepliesRequest request)
    {
        return await _trackCommentService.GetCommentRepliesAsync(request);
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    public async Task<List<CommentResponse>> GetCommentThreadAsync(CommentThreadRequest request)
    {
        return await _trackCommentService.GetCommentThreadAsync(request);
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    public async Task<int> GetCommentDepthAsync(string commentId)
    {
        return await _trackCommentService.GetCommentDepthAsync(commentId);
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    public async Task<bool> IsCommentInThreadAsync(string commentId, string threadRootId)
    {
        return await _trackCommentService.IsCommentInThreadAsync(commentId, threadRootId);
    }
    #endregion
}
