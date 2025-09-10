using EkofyApp.Application.ServiceInterfaces.Coupons;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Coupons;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class CouponQuery(ICouponCustomService couponCustomService)
{
    private readonly ICouponCustomService _couponCustomService = couponCustomService;

    public IQueryable<Coupon> GetAllCoupons()
    {
        return _couponCustomService.GetAllCoupons();
    }
}
