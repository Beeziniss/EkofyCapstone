using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Comments;

public sealed class CommentMutationExtension : ObjectTypeExtension<CommentMutation>
{
    protected override void Configure(IObjectTypeDescriptor<CommentMutation> descriptor)
    {
        descriptor.Field(x => x.CreateCommentAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(x => x.UpdateCommentAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(x => x.DeleteCommentAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);
    }
}
