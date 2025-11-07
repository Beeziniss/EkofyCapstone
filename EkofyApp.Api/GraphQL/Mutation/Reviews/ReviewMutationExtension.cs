using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Reviews;

public sealed class ReviewMutationExtension : ObjectTypeExtension<ReviewMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ReviewMutation> descriptor)
    {
        descriptor.Field(f => f.CreateReviewAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(f => f.UpdateReviewAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(f => f.DeleteReviewSoftAsync(default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);

        descriptor.Field(f => f.DeleteReviewHardAsync(default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);
    }
}
