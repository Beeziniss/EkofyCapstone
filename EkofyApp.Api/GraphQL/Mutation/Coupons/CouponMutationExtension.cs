using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Coupons;

public sealed class CouponMutationExtension : ObjectTypeExtension<CouponMutation>
{
    protected override void Configure(IObjectTypeDescriptor<CouponMutation> descriptor)
    {
        // Configure the CouponMutation type here if needed
        descriptor.Field(x => x.CreateCouponAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.DeprecateCouponAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.DeleteCouponAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);
    }
}
