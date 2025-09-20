using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Coupons;

public sealed class CouponQueryExtension : ObjectTypeExtension<CouponQuery>
{
    protected override void Configure(IObjectTypeDescriptor<CouponQuery> descriptor)
    {
        // Configure the CouponQuery type here if needed
        descriptor.Field(x => x.GetAllCoupons())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}
