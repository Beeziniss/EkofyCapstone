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

    // TODO: Tạo entitlement mới
}
