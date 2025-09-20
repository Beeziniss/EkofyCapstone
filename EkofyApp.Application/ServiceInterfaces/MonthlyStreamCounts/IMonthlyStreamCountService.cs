using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.MonthlyStreamCounts;
public interface IMonthlyStreamCountService
{
    IQueryable<MonthlyStreamCount> GetMonthlyStreamCounts();
    Task UpsertMonthlyStreamCountAsync(string trackId, long streamCount, int month, int year, CancellationToken cancellationToken = default);
}
