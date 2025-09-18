using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Policies;
public sealed class LegalPolicyService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService) : ILegalPolicyService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public IQueryable<LegalPolicy> GetLegalPolicies()
    {
        return _unitOfWork.GetCollection<LegalPolicy>().AsQueryable();
    }

    public async Task CreateLegalPolicyAsync(CreateLegalPolicyRequest createLegalPolicyRequest)
    {
        long currentVersion = await _unitOfWork.GetCollection<LegalPolicy>()
            .Find(_ => true)
            .SortByDescending(x => x.Version)
            .Project(x => x.Version)
            .FirstOrDefaultAsync();

        await _unitOfWork.GetCollection<LegalPolicy>().InsertOneAsync(new()
        {
            Name = createLegalPolicyRequest.Name,
            Content = createLegalPolicyRequest.Content,
            Version = ++currentVersion,
            IsActive = createLegalPolicyRequest.IsActive,
        });
    }

    // Method for initializing policy when the system is first set up
    public async Task InitializePolicyAsync()
    {
        LegalPolicy currentPolicy = await _unitOfWork.GetCollection<LegalPolicy>().Find(x => x.IsActive == true).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any legal policy is active");

        Dictionary<string, string?> policyDictionary = new()
        {
            { "name", currentPolicy.Name },
            { "content", currentPolicy.Content },
            { "version", currentPolicy.Version.ToString() },
            { "is_active", currentPolicy.IsActive.ToString() }
        };

        await _redisCacheService.HashSetAsync("legal_policy:active", policyDictionary);
    }
}
