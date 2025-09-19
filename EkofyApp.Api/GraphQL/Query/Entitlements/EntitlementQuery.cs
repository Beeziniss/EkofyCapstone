using EkofyApp.Application.ServiceInterfaces.Entitlements;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Entitlements;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class EntitlementQuery(IEntitlementService entitlementService)
{
    private readonly IEntitlementService _entitlementService = entitlementService;

    public IQueryable<Entitlement> GetEntitlements()
    {
        return _entitlementService.GetEntitlements();
    }
}
