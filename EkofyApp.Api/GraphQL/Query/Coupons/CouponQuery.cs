using EkofyApp.Application.ServiceInterfaces.Coupons;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Coupons;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class CouponQuery(ICouponCustomService couponCustomService)
{
    private readonly ICouponCustomService _couponCustomService = couponCustomService;

    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Coupon>]
    public IQueryable<Coupon> GetCoupons()
    {
        return _couponCustomService.GetAllCoupons().AsQueryable();
    }
}
