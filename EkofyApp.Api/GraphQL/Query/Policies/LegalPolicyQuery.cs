using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Policies;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class LegalPolicyQuery(ILegalPolicyService legalPolicyService)
{
    private readonly ILegalPolicyService _legalPolicyService = legalPolicyService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<LegalPolicy>]
    public IQueryable<LegalPolicy> GetLegalPolicies()
    {
        return _legalPolicyService.GetLegalPolicies();
    }
}
