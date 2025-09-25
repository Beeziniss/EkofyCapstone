
using EkofyApp.Application.Models.Coupons;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Coupons;
public interface ICouponCustomService
{
    Task CreateCouponAsync(CreateCouponRequest createCouponRequest);
    Task DeleteCouponAsync(IEnumerable<string> couponIds);
    Task DeprecateCouponAsync(IEnumerable<string> couponIds);
    IQueryable<Coupon> GetAllCoupons();
    Task<IEnumerable<Coupon>> GetAllCouponsIE();
    Task<bool> IsCouponCodeExistsAsync(string code);
}
