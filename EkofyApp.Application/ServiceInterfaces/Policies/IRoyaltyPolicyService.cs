
using EkofyApp.Application.Models.Policies;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Policies;
public interface IRoyaltyPolicyService
{
    Task CreateRoyalPolicyAsync(CreateRoyalPolicyRequest createRoyalPolicyRequest);
    IQueryable<RoyaltyPolicy> GetRoyaltyPolicies();
    Task InitializePolicyAsync();
    Task SeedDataAsync();
}
