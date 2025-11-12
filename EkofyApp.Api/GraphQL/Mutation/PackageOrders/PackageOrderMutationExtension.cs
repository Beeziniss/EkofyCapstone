using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.PackageOrders;

public sealed class PackageOrderMutationExtension : ObjectTypeExtension<PackageOrderMutation>
{
    protected override void Configure(IObjectTypeDescriptor<PackageOrderMutation> descriptor)
    {
        descriptor.Field(x => x.SubmitDeliverytAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(x => x.ApproveDeliveryAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(x => x.SendRedoRequestAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(x => x.CreateReviewAsync(default!))
            .Authorize(HelperRoleBase.ListenerRolesArray);

        descriptor.Field(x => x.UpdateReviewAsync(default!))
            .Authorize(HelperRoleBase.ListenerModeratorAdminRolesArray);

        descriptor.Field(x => x.DeleteReviewHardAsync(default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);
    }
}
