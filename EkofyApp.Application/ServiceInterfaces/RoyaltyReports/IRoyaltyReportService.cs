
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
public interface IRoyaltyReportService
{
    Task GenerateMonthlyRoyaltyReportsAsync(int month, int year, int limit = 100, CancellationToken ct = default);
    IQueryable<RoyaltyReport> GetRoyaltyReports();
    Task<long> GetTotalCountOfRoyaltyReportsAsync(int month, int year, CancellationToken ct = default);
    
    // Payout methods
    Task<bool> ProcessPayoutForArtistAsync(string artistId, decimal amount, bool isInstant = false, CancellationToken ct = default);
    Task<bool> ProcessPayoutsForAllArtistsAsync(int month, int year, bool isInstant = false, CancellationToken ct = default);
}
