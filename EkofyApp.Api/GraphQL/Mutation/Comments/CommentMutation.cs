using EkofyApp.Application.Models.TrackComments;
using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Api.GraphQL.Mutation.Comments;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class CommentMutation(ITrackCommentService trackCommentService)
{
    private readonly ITrackCommentService _trackCommentService = trackCommentService;

    #region Track Comment
    public async Task<bool> CreateTrackCommentAsync(CreateTrackCommentRequest request)
    {
        // Set CommentType to Track for backward compatibility
        request = request with { CommentType = CommentType.Track };
        await _trackCommentService.CreateCommentAsync(request);
        return true;
    }

    public async Task<bool> UpdateTrackCommentAsync(UpdateTrackCommentRequest request)
    {
        await _trackCommentService.UpdateCommentAsync(request);
        return true;
    }

    public async Task<bool> DeleteTrackCommentAsync(DeleteTrackCommentRequest request)
    {
        await _trackCommentService.DeleteCommentAsync(request);
        return true;
    }
    #endregion
}
