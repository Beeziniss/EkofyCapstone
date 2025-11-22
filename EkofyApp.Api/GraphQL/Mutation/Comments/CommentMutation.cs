using EkofyApp.Application.Models.Comments;
using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;

namespace EkofyApp.Api.GraphQL.Mutation.Comments;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class CommentMutation(ICommentService trackCommentService, IUserService userService)
{
    private readonly ICommentService _commentService = trackCommentService;
    private readonly IUserService _userService = userService;

    public async Task<bool> CreateCommentAsync(CreateCommentRequest request)
    {
        bool hasAnyRestriction = await _userService.CheckMultipleRestrictionsAsync(RestrictionAction.Comment);
        if (hasAnyRestriction)
        {
            throw new UnauthorizedCustomException("You are restricted from commenting.");
        }

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
