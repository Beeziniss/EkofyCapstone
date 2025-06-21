namespace EkofyApp.Api.GraphQL.Mutation.Artist;

public class ArtistMutationType : ObjectType<ArtistMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ArtistMutation> descriptor)
    {
        //descriptor.Authorize();
        // Uncomment and implement the method if needed
        // descriptor.Field(x => x.CreateArtistAsync(default!)).AllowAnonymous();
    }
}