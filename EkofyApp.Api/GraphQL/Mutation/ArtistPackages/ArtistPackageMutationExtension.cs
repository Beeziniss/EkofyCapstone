namespace EkofyApp.Api.GraphQL.Mutation.ArtistPackages
{
    public class ArtistPackageMutationExtension : ObjectTypeExtension<ArtistPackageMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<ArtistPackageMutation> descriptor)
        {
            // Configure the ArtistPackageMutation type here if needed
            descriptor.Field(x => x.CreateArtistPackageAsync(default!))
                .Authorize(roles: "Artist");

            //descriptor.Field(x => x.UpdateArtistPackageAsync(default!))
            //    .Authorize(roles: "Artist");

            descriptor.Field(x => x.ChangeArtistPackageStatusAsync(default!))
                .Authorize(roles: "Artist");

            descriptor.Field(x => x.ApproveArtistPackageAsync(default!))
                .Authorize(roles: "Moderator");

        }
    }
}
