using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Policies;
public sealed class RoyaltyPolicyService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService) : IRoyaltyPolicyService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public IQueryable<RoyaltyPolicy> GetRoyaltyPolicies()
    {
        return _unitOfWork.GetCollection<RoyaltyPolicy>().AsQueryable();
    }

    public async Task CreateRoyalPolicyAsync(CreateRoyalPolicyRequest createRoyalPolicyRequest)
    {
        long currentVersion = await _unitOfWork.GetCollection<RoyaltyPolicy>()
            .Find(_ => true)
            .SortByDescending(x => x.Version)
            .Project(x => x.Version)
            .FirstOrDefaultAsync();

        await _unitOfWork.GetCollection<RoyaltyPolicy>().InsertOneAsync(new()
        {
            RatePerStream = createRoyalPolicyRequest.RatePerStream,
            Currency = createRoyalPolicyRequest.Currency,
            RecordingPercentage = createRoyalPolicyRequest.RecordingPercentage,
            WorkPercentage = createRoyalPolicyRequest.WorkPercentage,
            Version = ++currentVersion,
            IsActive = createRoyalPolicyRequest.IsActive
        });
    }

    // Method for initializing policy when the system is first set up
    public async Task InitializePolicyAsync()
    {
        RoyaltyPolicy currentPolicy = await _unitOfWork.GetCollection<RoyaltyPolicy>().Find(x => x.IsActive == true).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any royal policy is active");

        Dictionary<string, string?> policyDictionary = new()
        {
            { "rate_per_stream", currentPolicy.RatePerStream.ToString() },
            { "currency", currentPolicy.Currency.ToString() },
            { "recording_percentage", currentPolicy.RecordingPercentage.ToString() },
            { "work_percentage", currentPolicy.WorkPercentage.ToString() },
            { "version", currentPolicy.Version.ToString() },
            { "is_active", currentPolicy.IsActive.ToString() }
        };

        await _redisCacheService.HashSetAsync("royalty_policy:active", policyDictionary);
    }

    public async Task SeedDataAsync()
    {
        await _unitOfWork.GetCollection<RoyaltyPolicy>().InsertOneAsync(new()
        {
            RatePerStream = 10.0m,
            Currency = CurrencyType.vnd,
            RecordingPercentage = 70m,
            WorkPercentage = 30m,
            Version = 1,
            IsActive = true
        });
    }
}
