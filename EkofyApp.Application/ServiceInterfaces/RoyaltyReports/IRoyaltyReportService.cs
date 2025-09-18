
namespace EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
public interface IRoyaltyReportService
{
    Task GenerateMonthlyRoyaltyReportsAsync(int month, int year, CancellationToken ct = default);
}
