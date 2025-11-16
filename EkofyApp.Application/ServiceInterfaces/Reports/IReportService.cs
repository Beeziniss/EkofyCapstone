using EkofyApp.Application.Models.Reports;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Reports;

public interface IReportService
{
    Task AssignReportToModeratorAsync(string reportId, string moderatorId);
    Task CreateReportAsync(CreateReportRequest request);
    Task<bool> DeleteReportAsync(string reportId);
    Task<bool> EscalateReportAsync(string reportId);
    IQueryable<Report> GetReports();
    Task<ReportStatisticsResponse> GetReportStatisticsAsync();
    Task ProcessReportAsync(ProcessReportRequest request);
    Task RemoveExpiredRestrictionAsync(string userId);
    Task RestoreContentAsync(string reportId);
    Task<bool> UnbanUserAsync(string reportId);
    Task<bool> UpdateReportPriorityAsync(string reportId, string priority);
}
