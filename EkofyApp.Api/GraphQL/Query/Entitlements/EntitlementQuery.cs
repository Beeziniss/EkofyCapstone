using EkofyApp.Application.ServiceInterfaces.Entitlements;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Entitlements;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class EntitlementQuery(IEntitlementService entitlementService)
{
    private readonly IEntitlementService _entitlementService = entitlementService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Entitlement>]
    public IQueryable<Entitlement> GetEntitlements()
    {
        return _entitlementService.GetEntitlements();
    }
}
