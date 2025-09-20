using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Artists;

public sealed class ArtistQueryExtension : ObjectTypeExtension<ArtistQuery>
{
    protected override void Configure(IObjectTypeDescriptor<ArtistQuery> descriptor)
    {
        // You can define fields here if needed
        // For example:
        // descriptor.Field(x => x.GetArtistById(default)).Description("Returns an artist by its ID.");
        // descriptor.Field(x => x.GetAllArtists()).Description("Returns all artists.");

        descriptor.Field(x => x.GetArtists())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}
