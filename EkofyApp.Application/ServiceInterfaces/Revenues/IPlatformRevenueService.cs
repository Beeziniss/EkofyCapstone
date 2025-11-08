using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Revenues;
public interface IPlatformRevenueService
{
    Task<PlatformRevenue> ComputePlatformRevenueAsync();
    IQueryable<PlatformRevenue> GetPlatformRevenues();
}
