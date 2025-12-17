using EkofyApp.Application.Models.Policies;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Tracks;

public interface IFingerprintConfidencePolicyService
{
    Task<FingerprintConfidencePolicy> GetPolicyAsync();
    Task InitializePolicyAsync();
    Task SeedDataAsync();
    Task UpdatePolicyAsync(UpdateFingerprintConfidencePolicyRequest updateRequest);
}
