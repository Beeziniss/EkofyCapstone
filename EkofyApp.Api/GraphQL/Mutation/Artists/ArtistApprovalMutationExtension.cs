using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Artists;

public sealed class ArtistApprovalMutationExtension : ObjectTypeExtension<ArtistApprovalMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ArtistApprovalMutation> descriptor)
    {
        // Only moderators can approve/reject artist registrations
        descriptor.Field(x => x.ApproveArtistRegistrationAsync(default!))
            .Authorize(HelperRoleBase.ModeratorRolesArray);

        descriptor.Field(x => x.RejectArtistRegistrationAsync(default!))
            .Authorize(HelperRoleBase.ModeratorRolesArray);
    }
}