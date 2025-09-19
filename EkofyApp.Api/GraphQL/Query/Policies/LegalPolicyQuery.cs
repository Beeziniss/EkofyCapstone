using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Policies;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class LegalPolicyQuery(ILegalPolicyService legalPolicyService)
{
    private readonly ILegalPolicyService _legalPolicyService = legalPolicyService;

    public IQueryable<LegalPolicy> GetLegalPolicies()
    {
        return _legalPolicyService.GetLegalPolicies();
    }
}
