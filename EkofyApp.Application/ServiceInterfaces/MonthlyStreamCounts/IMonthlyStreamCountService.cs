using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.MonthlyStreamCounts;
public interface IMonthlyStreamCountService
{
    IQueryable<MonthlyStreamCount> GetMonthlyStreamCounts();
}
