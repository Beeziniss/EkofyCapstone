using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Policies;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class RoyaltyPolicyQuery(IRoyaltyPolicyService royaltyPolicyService)
{
    private readonly IRoyaltyPolicyService _royaltyPolicyService = royaltyPolicyService;

    public IQueryable<RoyaltyPolicy> GetRoyaltyPolicies()
    {
        return _royaltyPolicyService.GetRoyaltyPolicies();
    }
}
