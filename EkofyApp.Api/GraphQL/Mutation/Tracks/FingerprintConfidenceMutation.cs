using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces.Tracks;

namespace EkofyApp.Api.GraphQL.Mutation.Tracks;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class FingerprintConfidenceMutation(IFingerprintConfidencePolicyService fingerprintConfidencePolicyService)
{
    private readonly IFingerprintConfidencePolicyService _fingerprintConfidencePolicyService = fingerprintConfidencePolicyService;

    public async Task<bool> UpdateFingerprintConfidencePolicyAsync(UpdateFingerprintConfidencePolicyRequest updateRequest)
    {
        await _fingerprintConfidencePolicyService.UpdatePolicyAsync(updateRequest);
        return true;
    }
}
