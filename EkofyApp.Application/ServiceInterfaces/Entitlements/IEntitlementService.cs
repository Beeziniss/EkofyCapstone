
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Entitlements;
public interface IEntitlementService
{
    IQueryable<Entitlement> GetEntitlements();
    Task SeedDataAsync();
}
