using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class FingerprintConfidenceQuery(IFingerprintConfidencePolicyService fingerprintConfidencePolicyService)
{
    private readonly IFingerprintConfidencePolicyService fingerprintConfidencePolicyService = fingerprintConfidencePolicyService;

    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<FingerprintConfidencePolicy>]
    public async Task<FingerprintConfidencePolicy> GetFingerprintConfidencePolicyAsync()
    {
        return await fingerprintConfidencePolicyService.GetPolicyAsync();
    }
}
