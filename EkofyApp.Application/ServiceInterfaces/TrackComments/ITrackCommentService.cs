using EkofyApp.Application.Models.TrackComments;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.TrackComments;

public interface ITrackCommentService
{
    IQueryable<Comment> GetTrackComments();
    Task CreateCommentAsync(CreateCommentRequest request);
    Task UpdateCommentAsync(UpdateTrackCommentRequest request);
    Task DeleteCommentAsync(DeleteCommentRequest request);
    
    // Enhanced hierarchical commenting methods
    Task<ThreadedCommentsResponse> GetThreadedCommentsAsync(ThreadedCommentsRequest request);
    Task<CommentRepliesResponse> GetCommentRepliesAsync(CommentRepliesRequest request);
    Task<List<CommentResponse>> GetCommentThreadAsync(CommentThreadRequest request);
    
    // Utility methods
    Task<int> GetCommentDepthAsync(string commentId);
    Task<bool> IsCommentInThreadAsync(string commentId, string threadRootId);
}