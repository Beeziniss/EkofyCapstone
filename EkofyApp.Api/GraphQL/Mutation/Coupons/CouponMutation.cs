using EkofyApp.Application.Models.Coupons;
using EkofyApp.Application.ServiceInterfaces.Coupons;

namespace EkofyApp.Api.GraphQL.Mutation.Coupons;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class CouponMutation(ICouponCustomService couponCustomService)
{
    private readonly ICouponCustomService _couponCustomService = couponCustomService;

    public async Task<bool> CreateCouponAsync(CreateCouponRequest createCouponRequest)
    {
        if (await _couponCustomService.IsCouponCodeExistsAsync(createCouponRequest.Code))
        {
            throw new GraphQLException(new Error("Coupon code already exists.", "COUPON_CODE_EXISTS"));
        }

        await _couponCustomService.CreateCouponAsync(createCouponRequest);
        return true;
    }

    public async Task<bool> DeprecateCouponAsync(IEnumerable<string> couponIds)
    {
        await _couponCustomService.DeprecateCouponAsync(couponIds);
        return true;
    }

    public async Task<bool> DeleteCouponAsync(IEnumerable<string> couponIds)
    {
        await _couponCustomService.DeleteCouponAsync(couponIds);
        return true;
    }
}
