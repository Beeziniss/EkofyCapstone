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
    // New hierarchical comment queries
    [UseProjection]
    public async Task<ThreadedCommentsResponse> GetThreadedCommentsAsync(ThreadedCommentsRequest request)
    {
        return await _trackCommentService.GetThreadedCommentsAsync(request);
    }

    [UseProjection]
    public async Task<CommentRepliesResponse> GetCommentRepliesAsync(CommentRepliesRequest request)
    {
        return await _trackCommentService.GetCommentRepliesAsync(request);
    }

    [UseProjection]
    public async Task<List<CommentResponse>> GetCommentThreadAsync(CommentThreadRequest request)
    {
        return await _trackCommentService.GetCommentThreadAsync(request);
    }

    public async Task<int> GetCommentDepthAsync(string commentId)
    {
        return await _trackCommentService.GetCommentDepthAsync(commentId);
    }

    public async Task<bool> IsCommentInThreadAsync(string commentId, string threadRootId)
    {
        return await _trackCommentService.IsCommentInThreadAsync(commentId, threadRootId);
    }
    #endregion
}
