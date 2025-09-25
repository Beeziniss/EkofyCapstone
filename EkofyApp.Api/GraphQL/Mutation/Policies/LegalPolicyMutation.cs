using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces.Policies;

namespace EkofyApp.Api.GraphQL.Mutation.Policies;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class LegalPolicyMutation(ILegalPolicyService legalPolicyService)
{
    private readonly ILegalPolicyService _legalPolicyService = legalPolicyService;

    public async Task<bool> CreateLegalPolicyAsync(CreateLegalPolicyRequest createLegalPolicyRequest)
    {
        await _legalPolicyService.CreateLegalPolicyAsync(createLegalPolicyRequest);
        return true;
    }
}
