using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(ArtistRevenue))]
public sealed class ArtistRevenueResolver
{
    public async Task<decimal> GetNetEarningsAsync([Parent] ArtistRevenue artistRevenue, [Service] IUnitOfWork unitOfWork, [Service] IRedisCacheService redisCacheService)
    {
        // Lấy platform fee percentage từ Redis
        string platformFeePercentageStr = await redisCacheService.HashGetAsync("escrow_commission_policy:active", "platform_fee_percentage") ?? await unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .Project(x => x.PlatformFeePercentage.ToString())
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found active escrow commission policy.");
        decimal platformFeePercentage = Convert.ToDecimal(platformFeePercentageStr);

        return artistRevenue.RoyaltyEarnings + (artistRevenue.ServiceRevenue * (1 - platformFeePercentage)) - artistRevenue.RefundAmount;
    }
}
