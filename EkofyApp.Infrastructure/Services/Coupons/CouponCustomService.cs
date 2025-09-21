using EkofyApp.Application.Models.Coupons;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Coupons;
using EkofyApp.Domain.Enums.Coupons;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;
using Stripe;

namespace EkofyApp.Infrastructure.Services.Coupons;
public sealed class CouponCustomService(IUnitOfWork unitOfWork) : ICouponCustomService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<EntityCoupon> GetAllCoupons()
    {
        return _unitOfWork.GetCollection<EntityCoupon>().AsQueryable();
    }

    public async Task<IEnumerable<EntityCoupon>> GetAllCouponsIE()
    {
        return await _unitOfWork.GetCollection<EntityCoupon>()
            .Find(_ => true)
            .ToListAsync();
    }

    public async Task<bool> IsCouponCodeExistsAsync(string code)
    {
        string existingCoupon = await _unitOfWork.GetCollection<EntityCoupon>()
            .Find(x => x.Code == code.ToLowerInvariant())
            .Project(x => x.Id)
            .FirstOrDefaultAsync();

        return !string.IsNullOrEmpty(existingCoupon);
    }

    public async Task CreateCouponAsync(CreateCouponRequest createCouponRequest)
    {
        CouponCreateOptions options = new()
        {
            Name = createCouponRequest.Name,
            Duration = createCouponRequest.Duration.ToString(),
            PercentOff = createCouponRequest.PercentOff,
        };
        CouponService service = new();
        Coupon stripeCoupon = service.Create(options);

        if(service.Get(stripeCoupon.Id) == null)
        {
            throw new ExternalServiceCustomException("Failed to create coupon in Stripe.");
        }

        await _unitOfWork.GetCollection<EntityCoupon>().InsertOneAsync(new EntityCoupon()
        {
            StripeCouponId = stripeCoupon.Id,
            Name = createCouponRequest.Name,
            Description = createCouponRequest.Description,
            Code = createCouponRequest.Code,
            PercentOff = createCouponRequest.PercentOff,
            Duration = createCouponRequest.Duration,
            Purpose = createCouponRequest.Purpose,
            Status = createCouponRequest.Status,
        });
    }

    public async Task DeprecateCouponAsync(IEnumerable<string> couponIds)
    {
        UpdateResult updateResult = await _unitOfWork.GetCollection<EntityCoupon>()
            .UpdateManyAsync(x => couponIds.Contains(x.Id) && x.Status == CouponStatus.Active,
                Builders<EntityCoupon>.Update.Set(x => x.Status, CouponStatus.Deprecated));
        if(updateResult.MatchedCount == 0)
        {
            throw new NotFoundCustomException("No coupons were found to deprecate. They might not exist or are already inactive.");
        }
        if (updateResult.ModifiedCount == 0)
        {
            throw new NotFoundCustomException("No coupons were deprecated.");
        }
    }

    public async Task DeleteCouponAsync(IEnumerable<string> couponIds)
    {
        DeleteResult deleteResult = await _unitOfWork.GetCollection<EntityCoupon>()
            .DeleteManyAsync(x => couponIds.Contains(x.Id) && (x.Status == CouponStatus.Deprecated || x.Status == CouponStatus.Inactive || x.Status == CouponStatus.Expired));
        if(deleteResult.DeletedCount == 0)
        {
            throw new UnprocessableEntityCustomException("No coupons were deleted. They might not exist or are not inactive/deprecated/expired.");
        }
    }
}
