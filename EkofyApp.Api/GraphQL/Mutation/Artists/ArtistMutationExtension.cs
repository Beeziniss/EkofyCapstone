using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Artists;

public class ArtistMutationExtension : ObjectTypeExtension<ArtistMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ArtistMutation> descriptor)
    {
        descriptor.Field(f => f.UpdateProfileAsync(default!))
            .Authorize(HelperRoleBase.ArtistRolesArray);

        descriptor.Field(x => x.ApproveArtistRegistrationAsync(default!))
            .Authorize(HelperRoleBase.ModeratorRolesArray);

        descriptor.Field(x => x.RejectArtistRegistrationAsync(default!))
            .Authorize(HelperRoleBase.ModeratorRolesArray);
    }
}