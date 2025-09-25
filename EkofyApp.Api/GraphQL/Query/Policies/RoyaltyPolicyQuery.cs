using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Policies;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class RoyaltyPolicyQuery(IRoyaltyPolicyService royaltyPolicyService)
{
    private readonly IRoyaltyPolicyService _royaltyPolicyService = royaltyPolicyService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<RoyaltyPolicy>]
    public IQueryable<RoyaltyPolicy> GetRoyaltyPolicies()
    {
        return _royaltyPolicyService.GetRoyaltyPolicies();
    }
}
