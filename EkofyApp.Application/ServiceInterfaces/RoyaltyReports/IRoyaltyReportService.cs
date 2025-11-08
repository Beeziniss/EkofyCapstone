
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
public interface IRoyaltyReportService
{
    Task GenerateMonthlyRoyaltyReportsAsync(int month, int year, int limit = 100, CancellationToken ct = default);
    IQueryable<RoyaltyReport> GetRoyaltyReports();
    Task<long> GetTotalCountOfRoyaltyReportsAsync(int month, int year, CancellationToken ct = default);
}
