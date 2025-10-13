using EkofyApp.Application.Models.TrackComments;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.TrackComments;

public interface ITrackCommentService
{
    IQueryable<TrackComment> GetTrackComments();
    Task CreateCommentAsync(CreateTrackCommentRequest request);
    Task UpdateCommentAsync(UpdateTrackCommentRequest request);
    Task DeleteCommentAsync(DeleteTrackCommentRequest request);
    
    // Enhanced hierarchical commenting methods
    Task<ThreadedCommentsResponse> GetThreadedCommentsAsync(ThreadedCommentsRequest request);
    Task<CommentRepliesResponse> GetCommentRepliesAsync(CommentRepliesRequest request);
    Task<List<TrackCommentResponse>> GetCommentThreadAsync(CommentThreadRequest request);
    
    // Utility methods
    Task<int> GetCommentDepthAsync(string commentId);
    Task<bool> IsCommentInThreadAsync(string commentId, string threadRootId);
}