using EkofyApp.Application.Models.TrackComments;
using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Api.GraphQL.Mutation.Comments;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class CommentMutation(ICommentService trackCommentService)
{
    private readonly ICommentService _trackCommentService = trackCommentService;

    public async Task<bool> CreateTrackCommentAsync(CreateCommentRequest request)
    public async Task<bool> CreateCommentAsync(CreateCommentRequest request)
    {
        // Set CommentType to Track for backward compatibility
        await _trackCommentService.CreateCommentAsync(request);
        return true;
    }

    public async Task<bool> UpdateCommentAsync(UpdateTrackCommentRequest request)
    {
        await _trackCommentService.UpdateCommentAsync(request);
        return true;
    }

    public async Task<bool> DeleteCommentAsync(DeleteCommentRequest request)
    {
        await _trackCommentService.DeleteCommentAsync(request);
        return true;
    }
}
