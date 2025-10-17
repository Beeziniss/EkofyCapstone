using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Comments;

public sealed class CommentMutationExtension : ObjectTypeExtension<CommentMutation>
{
    protected override void Configure(IObjectTypeDescriptor<CommentMutation> descriptor)
    {
        descriptor.Field(x => x.CreateTrackCommentAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(x => x.UpdateTrackCommentAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(x => x.DeleteTrackCommentAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);
    }
}
