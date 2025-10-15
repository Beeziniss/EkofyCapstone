using EkofyApp.Application.Models.Reports;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Reports;

public interface IReportService
{
    Task AssignReportToModeratorAsync(string reportId, string moderatorId);
    Task CreateReportAsync(CreateReportRequest request);
    IQueryable<Report> GetReports();
    Task ProcessReportAsync(ProcessReportRequest request);
    Task RemoveExpiredRestrictionAsync(string userId);
}
