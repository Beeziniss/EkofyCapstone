using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using HotChocolate.Data;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(Artist))]
public sealed class ArtistsResolver
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUser([Parent] Artist artist, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<User>().AsQueryable().Where(x => x.Id == artist.UserId);
    }

    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Category> GetCategories([Parent] Artist artist, [Service] IUnitOfWork unitOfWork)
    {
        return unitOfWork.GetCollection<Category>().AsQueryable().Where(x => artist.CategoryIds.Contains(x.Id));
    }

    //public async Task<decimal> GetNetEarningsAsync([Parent] Artist artistRevenue, [Service] IUnitOfWork unitOfWork, [Service] IRedisCacheService redisCacheService)
    //{
    //    // Lấy platform fee percentage từ Redis
    //    string platformFeePercentageStr = await redisCacheService.HashGetAsync("escrow_commission_policy:active", "platform_fee_percentage") ?? await unitOfWork.GetCollection<EscrowCommissionPolicy>()
    //        .Find(x => x.Status == PolicyStatus.Active)
    //        .Project(x => x.PlatformFeePercentage.ToString())
    //        .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found active escrow commission policy.");
    //    decimal platformFeePercentage = Convert.ToDecimal(platformFeePercentageStr);

    //    return artistRevenue.RoyaltyEarnings + (artistRevenue.ServiceRevenue * (1 - platformFeePercentage)) - artistRevenue.RefundAmount;
    //}
}
