
using EkofyApp.Application.Models.Policies;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Policies;
public interface IEscrowCommissionPolicyService
{
    Task CreatePolicyAsync(CreateEscrowCommissionPolicyRequest createRequest);
    Task DowngradeVersionAsync(long? version = null);
    IQueryable<EscrowCommissionPolicy> GetEscrowCommissionPolicies();
    Task InitializePolicyAsync();
    Task SeedDataAsync();
    Task SwitchToLatestVersionAsync();
    Task UpdatePolicyAsync(UpdateEscrowCommissionPolicyRequest updateRequest);
}
