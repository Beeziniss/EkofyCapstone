using EkofyApp.Application.Models.Comments;
using EkofyApp.Application.ServiceInterfaces.TrackComments;

namespace EkofyApp.Api.GraphQL.Mutation.Comments;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class CommentMutation(ICommentService trackCommentService)
{
    private readonly ICommentService _commentService = trackCommentService;

    public async Task<bool> CreateCommentAsync(CreateCommentRequest request)
    {
        await _commentService.CreateCommentAsync(request);
        return true;
    }

    public async Task<bool> UpdateCommentAsync(UpdateTrackCommentRequest request)
    {
        await _commentService.UpdateCommentAsync(request);
        return true;
    }

    public async Task<bool> DeleteCommentAsync(DeleteCommentRequest request)
    {
        await _commentService.DeleteCommentAsync(request);
        return true;
    }
}
