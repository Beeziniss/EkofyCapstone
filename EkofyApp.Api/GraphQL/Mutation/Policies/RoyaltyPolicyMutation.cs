using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces.Policies;

namespace EkofyApp.Api.GraphQL.Mutation.Policies;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class RoyaltyPolicyMutation(IRoyaltyPolicyService royaltyPolicyService)
{
    private readonly IRoyaltyPolicyService _royaltyPolicyService = royaltyPolicyService;

    public async Task<bool> CreateRoyaltyPolicyAsync(CreateRoyalPolicyRequest createRoyalPolicyRequest)
    {
        await _royaltyPolicyService.CreateRoyalPolicyAsync(createRoyalPolicyRequest);
        return true;
    }

    public async Task<bool> DowngradeRoyaltyPolicyVersionAsync(long? version = null)
    {
        await _royaltyPolicyService.DowngradeVersionAsync(version);
        return true;
    }

    public async Task<bool> SeedRoyaltyPolicyDataAsync(string password)
    {
        if (password == "Tú đẹp trai")
        {
            await _royaltyPolicyService.SeedDataAsync();

            return true;
        }

        return false;
    }
}
