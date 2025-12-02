using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces.Policies;

namespace EkofyApp.Api.GraphQL.Mutation.Policies;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class EscrowComissionPolicyMutation(IEscrowCommissionPolicyService escrowCommissionPolicyService)
{
    private readonly IEscrowCommissionPolicyService _escrowCommissionPolicyService = escrowCommissionPolicyService;

    public async Task<bool> CreateEscrowCommissionPolicyAsync(CreateEscrowCommissionPolicyRequest createRequest)
    {
        await _escrowCommissionPolicyService.CreatePolicyAsync(createRequest);
        return true;
    }

    public async Task<bool> UpdateEscrowCommissionPolicyAsync(UpdateEscrowCommissionPolicyRequest updateRequest)
    {
        await _escrowCommissionPolicyService.UpdatePolicyAsync(updateRequest);
        return true;
    }

    public async Task<bool> DowngradeEscrowCommissionPolicyVersionAsync(long? version = null)
    {
        await _escrowCommissionPolicyService.DowngradeVersionAsync(version);
        return true;
    }

    public async Task<bool> SwitchEscrowCommissionPolicyToLatestVersionAsync()
    {
        await _escrowCommissionPolicyService.SwitchToLatestVersionAsync();
        return true;
    }

    public async Task<bool> SeedEscrowCommissionPolicyDataAsync(string password)
    {
        if (password == "Tú đẹp trai")
        {
            await _escrowCommissionPolicyService.SeedDataAsync();
            return true;
        }
        return false;
    }
}
