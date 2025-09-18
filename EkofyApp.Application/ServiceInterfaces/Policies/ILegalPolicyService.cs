using EkofyApp.Application.Models.Policies;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Policies;
public interface ILegalPolicyService
{
    Task CreateLegalPolicyAsync(CreateLegalPolicyRequest createLegalPolicyRequest);
    IQueryable<LegalPolicy> GetLegalPolicies();
    Task InitializePolicyAsync();
}
