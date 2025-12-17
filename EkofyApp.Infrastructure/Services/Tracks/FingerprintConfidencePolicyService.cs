using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Tracks;

public sealed class FingerprintConfidencePolicyService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService) : IFingerprintConfidencePolicyService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public async Task<FingerprintConfidencePolicy> GetPolicyAsync()
    {
        return await _unitOfWork.GetCollection<FingerprintConfidencePolicy>()
            .Find(_ => true)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("Fingerprint confidence policy not found.");
    }

    public async Task UpdatePolicyAsync(UpdateFingerprintConfidencePolicyRequest updateRequest)
    {
        FingerprintConfidencePolicy policy = await GetPolicyAsync();

        UpdateDefinitionBuilder<FingerprintConfidencePolicy> builder = Builders<FingerprintConfidencePolicy>.Update;
        
        UpdateDefinition<FingerprintConfidencePolicy> updateDefinition = builder.Combine(
            builder.Set(x => x.RejectThreshold, updateRequest.RejectThreshold),
            builder.Set(x => x.ManualReviewThreshold, updateRequest.ManualReviewThreshold)
        );

        await _unitOfWork.GetCollection<FingerprintConfidencePolicy>()
            .UpdateOneAsync(x => x.Id == policy.Id, updateDefinition);

        policy.RejectThreshold = updateRequest.RejectThreshold;
        policy.ManualReviewThreshold = updateRequest.ManualReviewThreshold;
        
        await UpdateRedisCacheAsync(policy);
    }

    public async Task InitializePolicyAsync()
    {
        FingerprintConfidencePolicy currentPolicy = await GetPolicyAsync();
        await UpdateRedisCacheAsync(currentPolicy);
    }

    public async Task SeedDataAsync()
    {
        long count = await _unitOfWork.GetCollection<FingerprintConfidencePolicy>()
            .CountDocumentsAsync(_ => true);

        if (count == 0)
        {
            await _unitOfWork.GetCollection<FingerprintConfidencePolicy>().InsertOneAsync(new()
            {
                RejectThreshold = 0.8,
                ManualReviewThreshold = 0.7,
            });

            await InitializePolicyAsync();
        }
    }

    private async Task UpdateRedisCacheAsync(FingerprintConfidencePolicy policy)
    {
        Dictionary<string, string?> policyDictionary = new()
        {
            { "reject_threshold", policy.RejectThreshold.ToString() },
            { "manual_review_threshold", policy.ManualReviewThreshold.ToString() }
        };

        await _redisCacheService.HashSetAsync("fingerprint_confidence_policy", policyDictionary);
    }
}
