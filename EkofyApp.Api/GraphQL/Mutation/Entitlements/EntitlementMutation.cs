using EkofyApp.Application.Models.Entitlements;
using EkofyApp.Application.ServiceInterfaces.Entitlements;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Mutation.Entitlements;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class EntitlementMutation(IEntitlementService entitlementService)
{
    private readonly IEntitlementService _entitlementService = entitlementService;

    public IQueryable<Entitlement> GetEntitlements()
    {
        return _entitlementService.GetEntitlements();
    }

    public async Task<bool> SeedEntitlementsAsync()
    {
        await _entitlementService.SeedDataAsync();
        return true;
    }

    public async Task<bool> CreateEntitlementAsync(CreateEntitlementRequest createEntitlementRequest)
    {
        await _entitlementService.CreateEntitlementAsync(createEntitlementRequest);
        return true;
    }

    public async Task<long> GetEntitlementUserCountAsync(string code)
    {
        return await _entitlementService.GetEntitlementUserCount(code);
    }

    public async Task<bool> DeactiveEntitlementAsync(string code)
    {
        await _entitlementService.DeactiveEntitlementAsync(code);
        return true;
    }

    public async Task<bool> ReactiveEntitlementAsync(string code)
    {
        await _entitlementService.ReactiveEntitlementAsync(code);
        return true;
    }
}
