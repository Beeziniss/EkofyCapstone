using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Policies;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class EscrowCommissionPolicyQuery(IEscrowCommissionPolicyService escrowCommissionPolicyService)
{
    private readonly IEscrowCommissionPolicyService _escrowCommissionPolicyService = escrowCommissionPolicyService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<EscrowCommissionPolicy>]
    public IQueryable<EscrowCommissionPolicy> GetEscrowCommissionPolicies()
    {
        return _escrowCommissionPolicyService.GetEscrowCommissionPolicies();
    }
}
