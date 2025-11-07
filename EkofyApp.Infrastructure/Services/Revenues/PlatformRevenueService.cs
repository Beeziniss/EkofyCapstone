using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Revenues;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Revenues;
public sealed class PlatformRevenueService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService) : IPlatformRevenueService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public IQueryable<PlatformRevenue> GetPlatformRevenues()
    {
        return _unitOfWork.GetCollection<PlatformRevenue>().AsQueryable();
    }

    public async Task<PlatformRevenue> ComputePlatformRevenueAsync()
    {
        // Tính tổng doanh thu từ subscription
        IEnumerable<decimal> subscriptionAmounts = await _unitOfWork.GetCollection<Invoice>()
                                            .Find(x => x.SubscriptionSnapshot != null && x.OneOffSnapshot == null)
                                            .Project(x => x.Amount)
                                            .ToListAsync();

        decimal totalSubscriptionRevenue = subscriptionAmounts.Sum();


        // Lấy platform fee percentage từ Redis
        string platformFeePercentageStr = await _redisCacheService.HashGetAsync("escrow_commission_policy:active", "platform_fee_percentage") ?? await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .Project(x => x.PlatformFeePercentage.ToString())
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found active escrow commission policy."); ;
        decimal platformFeePercentage = Convert.ToDecimal(platformFeePercentageStr);

        // Tính tổng doanh thu từ commission
        IEnumerable<decimal> commissionAmounts = await _unitOfWork.GetCollection<Invoice>()
            .Find(x => x.OneOffSnapshot != null && x.OneOffSnapshot.OneOffType == OneOffType.Payment && x.SubscriptionSnapshot == null)
            .Project(x => x.Amount * (platformFeePercentage / 100m))
            .ToListAsync();

        decimal totalComissionRevenue = commissionAmounts.Sum();

        // Tỉnh tổng payout amount
        IEnumerable<decimal> payoutAmounts = await _unitOfWork.GetCollection<RoyaltyReport>()
                .Find(_ => true)
                .Project(x => x.TotalRoyaltyAmount)
                .ToListAsync();

        decimal totalAmountPayout = payoutAmounts.Sum();

        // Tính tổng refund amount
        IEnumerable<decimal> refundAmounts = await _unitOfWork.GetCollection<Invoice>()
                .Find(x => x.OneOffSnapshot != null && x.OneOffSnapshot.OneOffType == OneOffType.Refund && x.SubscriptionSnapshot == null)
                .Project(x => x.Amount)
                .ToListAsync();

        decimal totalRefundAmount = refundAmounts.Sum();

        PlatformRevenue platformRevenue = new()
        {
            Currency = CurrencyType.vnd,
            TotalSubscriptionRevenue = totalSubscriptionRevenue,
            TotalComissionRevenue = totalComissionRevenue,
            TotalPayoutAmount = totalAmountPayout,
            TotalRefundAmount = totalRefundAmount,
        };

        //await _unitOfWork.GetCollection<PlatformRevenue>().InsertOneAsync(platformRevenue);

        return platformRevenue;
    }
}
