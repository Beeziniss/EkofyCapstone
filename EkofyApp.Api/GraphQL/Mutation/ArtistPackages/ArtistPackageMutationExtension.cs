using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.ArtistPackages
{
    public class ArtistPackageMutationExtension : ObjectTypeExtension<ArtistPackageMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<ArtistPackageMutation> descriptor)
        {
            // Configure the ArtistPackageMutation type here if needed
            descriptor.Field(x => x.CreateArtistPackageAsync(default!))
                .Authorize(roles: HelperRoleBase.ArtistRolesArray);

            descriptor.Field(x => x.UpdateArtistPackageAsync(default!))
                .Authorize(roles: HelperRoleBase.ArtistRolesArray);

            descriptor.Field(x => x.ChangeArtistPackageStatusAsync(default!))
                .Authorize(roles: HelperRoleBase.ArtistRolesArray);

            descriptor.Field(x => x.DeleteArtistPackageAsync(default!))
                .Authorize(roles: HelperRoleBase.ArtistRolesArray);

        }
    }
}
