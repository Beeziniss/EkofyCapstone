using EkofyApp.Api.GraphQL.Query.Coupons;

namespace EkofyApp.Api.GraphQL.Query.ArtistPackages
{
    public class ArtistPackageQueryExtension : ObjectTypeExtension<ArtistPackageQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ArtistPackageQuery> descriptor)
        {
            // Configure the CouponQuery type here if needed
            descriptor.Field(x => x.GetArtistPackages())
                .AllowAnonymous()
                .UseProjection()
                .UseFiltering()
                .UseSorting();
        }
    }
}
