using EkofyApp.Api.GraphQL.Mutation.Requests;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.PackageOrders
{
    public class PackageOrderMutationExtension : ObjectTypeExtension<PackageOrderMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<PackageOrderMutation> descriptor)
        {
            descriptor.Field(f => f.SendRedoRequestAsync(default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);
            descriptor.Field(x => x.RefundPartiallyAsync(default!))
                .Authorize(HelperRoleBase.ModeratorAdminRolesArray);
            descriptor.Field(x => x.SubmitDeliverytAsync(default!))
                .Authorize(HelperRoleBase.ArtistRolesArray);
            descriptor.Field(x => x.ApproveDeliveryAsync(default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);
            descriptor.Field(x => x.AcceptRequestByArtistAsync(default!))
                .Authorize(HelperRoleBase.ArtistRolesArray);
            descriptor.Field(x => x.SwitchStatusByRequestorAsync(default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);

            descriptor.Field(x => x.CreateReviewAsync(default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);

            descriptor.Field(x => x.UpdateReviewAsync(default!))
                .Authorize(HelperRoleBase.ListenerModeratorAdminRolesArray);

            descriptor.Field(x => x.DeleteReviewHardAsync(default!))
                .Authorize(HelperRoleBase.ModeratorAdminRolesArray);
        }
    }
}
