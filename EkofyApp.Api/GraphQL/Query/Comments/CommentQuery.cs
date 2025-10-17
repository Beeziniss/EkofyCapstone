using EkofyApp.Application.ServiceInterfaces.TrackComments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Comments;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class CommentQuery(ITrackCommentService trackCommentService)
{
    private readonly ITrackCommentService _trackCommentService = trackCommentService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Comment>]
    public IQueryable<Comment> GetTrackComments()
    {
        return _trackCommentService.GetTrackComments();
    }
}
