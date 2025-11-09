using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Revenues;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Revenues;
public sealed class ArtistRevenueService(IUnitOfWork unitOfWork) : IArtistRevenueService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<ArtistRevenue> GetArtistRevenues()
    {
        return _unitOfWork.GetCollection<ArtistRevenue>().AsQueryable();
    }

    public async Task<ArtistRevenue> ComputeArtistRevenueByArtistIdAsync(string artistId)
    {
        string userId = await _unitOfWork.GetCollection<Artist>()
            .Find(x => x.Id == artistId)
            .Project(x => x.UserId)
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"User with ArtistId {artistId} not found.");

        // Tính tổng royalty earnings cho artist
        IEnumerable<decimal> royaltyEarnings = await _unitOfWork.GetCollection<RoyaltyReport>()
            .Find(x => x.RoyaltySplits.Any(rs => rs.UserId == userId))
            .Project(x => x.RoyaltySplits.Where(rs => rs.UserId == userId).Sum(rs => rs.Amount))
            .ToListAsync();

        decimal totalRoyaltyEarnings = royaltyEarnings.Sum();

        // Tính tổng service revenue cho artist
        IEnumerable<decimal> serviceRevenues = await _unitOfWork.GetCollection<Invoice>()
            .Find(x => x.OneOffSnapshot != null && x.OneOffSnapshot.OneOffType == OneOffType.Payment && x.SubscriptionSnapshot == null && x.UserId == userId)
            .Project(x => x.Amount)
            .ToListAsync();

        decimal totalServiceRevenue = serviceRevenues.Sum();

        // Tính tổng refund amount cho artist
        IEnumerable<decimal> refundAmounts = await _unitOfWork.GetCollection<Invoice>()
            .Find(x => x.OneOffSnapshot != null && x.OneOffSnapshot.OneOffType == OneOffType.Refund && x.SubscriptionSnapshot == null && x.UserId == userId)
            .Project(x => x.Amount)
            .ToListAsync();

        decimal totalRefundAmount = refundAmounts.Sum();

        ArtistRevenue artistRevenue = new()
        {
            UserId = userId,
            RoyaltyEarnings = totalRoyaltyEarnings,
            ServiceRevenue = totalServiceRevenue,
            RefundAmount = totalRefundAmount,
        };

        await _unitOfWork.GetCollection<ArtistRevenue>().InsertOneAsync(artistRevenue);

        return artistRevenue;
    }
}
