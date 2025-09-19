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

    public async Task DowngradeVersionAsync(long? version = null)
    {
        RoyaltyPolicy activePolicy = await _unitOfWork.GetCollection<RoyaltyPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .Project<RoyaltyPolicy>(Builders<RoyaltyPolicy>.Projection
                .Include(x => x.Id)
                .Include(x => x.Version))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any royal policy is active");

        // Kiểm tra version có hợp lệ không
        if (version.HasValue && version >= activePolicy.Version)
        {
            throw new BadRequestCustomException("The specified version must be less than the current active version.");
        }

        // Tìm phiên bản trước (nếu có)
        RoyaltyPolicy previousPolicy = await _unitOfWork.GetCollection<RoyaltyPolicy>()
            .Find(x => x.Version == (version.HasValue ? version : activePolicy.Version - 1))
            .Project<RoyaltyPolicy>(Builders<RoyaltyPolicy>.Projection
                .Exclude(x => x.EffectiveAt))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("No previous version available to downgrade to.");

        // Cập nhật trạng thái của các phiên bản
        UpdateDefinition<RoyaltyPolicy> updateActive = Builders<RoyaltyPolicy>.Update.Set(p => p.Status, PolicyStatus.Inactive);
        UpdateDefinition<RoyaltyPolicy> updateNext = Builders<RoyaltyPolicy>.Update.Set(p => p.Status, PolicyStatus.Active);
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            await _unitOfWork.GetCollection<RoyaltyPolicy>().UpdateOneAsync(session, p => p.Id == activePolicy.Id, updateActive);
            await _unitOfWork.GetCollection<RoyaltyPolicy>().UpdateOneAsync(session, p => p.Id == previousPolicy.Id, updateNext);
        });

        // Cập nhật lại cache trong Redis
        Dictionary<string, string?> policyDictionary = new()
        {
            { "rate_per_stream", previousPolicy.RatePerStream.ToString() },
            { "currency", previousPolicy.Currency.ToString() },
            { "recording_percentage", previousPolicy.RecordingPercentage.ToString() },
            { "work_percentage", previousPolicy.WorkPercentage.ToString() },
            { "version", previousPolicy.Version.ToString() },
            { "status", previousPolicy.Status.ToString() }
        };

        await _redisCacheService.HashSetAsync("royalty_policy:active", policyDictionary);
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
            Status = PolicyStatus.Pending,
        });

        // TODO: Gửi thông báo tới người dùng (giả lập log, thực tế có thể gửi noti, email,...)

        // TODO: Lên lịch công bố chính sách sau n ngày
    }

    // Method for initializing policy when the system is first set up
    public async Task InitializePolicyAsync()
    {
        RoyaltyPolicy currentPolicy = await _unitOfWork.GetCollection<RoyaltyPolicy>().Find(x => x.Status == PolicyStatus.Active).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any royal policy is active");

        Dictionary<string, string?> policyDictionary = new()
        {
            { "rate_per_stream", currentPolicy.RatePerStream.ToString() },
            { "currency", currentPolicy.Currency.ToString() },
            { "recording_percentage", currentPolicy.RecordingPercentage.ToString() },
            { "work_percentage", currentPolicy.WorkPercentage.ToString() },
            { "version", currentPolicy.Version.ToString() },
            { "status", currentPolicy.Status.ToString() }
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
            Status = PolicyStatus.Active,
        });
    }
}
