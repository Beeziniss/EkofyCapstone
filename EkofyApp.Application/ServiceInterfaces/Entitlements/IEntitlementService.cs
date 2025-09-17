
using EkofyApp.Application.Models.Entitlements;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Entitlements;
public interface IEntitlementService
{
    Task CreateEntitlementAsync(CreateEntitlementRequest createEntitlementRequest);
    Task DeactiveEntitlementAsync(string code);
    IQueryable<Entitlement> GetEntitlements();
    Task<long> GetEntitlementUserCount(string code);
    Task ReactiveEntitlementAsync(string code);
    Task SeedDataAsync();
}
