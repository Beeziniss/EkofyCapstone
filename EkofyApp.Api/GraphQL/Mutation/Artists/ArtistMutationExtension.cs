using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Artists;

public class ArtistMutationExtension : ObjectTypeExtension<ArtistMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ArtistMutation> descriptor)
    {
        descriptor.Field(f => f.UpdateArtistAsync(default!))
            .Authorize(HelperRoleBase.ArtistRolesArray);
    }
}