using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using Hangfire;
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
        await UpdateRedisCacheAsync(previousPolicy);

        // Gửi email thông báo tới người dùng về chính sách mới
        string content = $@"
            <ul style=""padding-left: 20px; margin: 0;"">
              <li><strong>Rate Per Stream:</strong> {previousPolicy.RatePerStream} vnd</li>
              <li><strong>Recording Percentage:</strong> {previousPolicy.RecordingPercentage}%</li>
              <li><strong>Work Percentage:</strong> {previousPolicy.WorkPercentage}%</li>
            </ul>
        ";

        foreach (User? user in await _unitOfWork.GetCollection<User>()
            .Find(x => x.Role == UserRole.Artist)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id)
                .Include(x => x.Email)
                .Include(x => x.FullName))
            .ToListAsync())
        {
            BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.UpdatePolicy, user.Email, user.FullName, user.Email, content));
        }
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
    }

    public async Task UpdateRoyalPolicyAsync(UpdateRoyalPolicyRequest updateRequest)
    {
        // Tìm policy đang pending theo version
        RoyaltyPolicy policy = await _unitOfWork.GetCollection<RoyaltyPolicy>()
            .Find(x => x.Version == updateRequest.Version)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"No pending policy found with version {updateRequest.Version}");

        List<UpdateDefinition<RoyaltyPolicy>> updates = [];
        UpdateDefinitionBuilder<RoyaltyPolicy> builder = Builders<RoyaltyPolicy>.Update;

        if (updateRequest.RatePerStream.HasValue)
        {
            updates.Add(builder.Set(x => x.RatePerStream, updateRequest.RatePerStream.Value));
        }

        if (updateRequest.Currency.HasValue)
        {
            updates.Add(builder.Set(x => x.Currency, updateRequest.Currency.Value));
        }

        if (updateRequest.RecordingPercentage.HasValue)
        {
            updates.Add(builder.Set(x => x.RecordingPercentage, updateRequest.RecordingPercentage.Value));
        }

        if (updateRequest.WorkPercentage.HasValue)
        {
            updates.Add(builder.Set(x => x.WorkPercentage, updateRequest.WorkPercentage.Value));
        }

        if (updates.Count == 0)
        {
            throw new BadRequestCustomException("No valid fields provided to update.");
        }

        UpdateDefinition<RoyaltyPolicy> updateDefinition = builder.Combine(updates);

        await _unitOfWork.GetCollection<RoyaltyPolicy>().UpdateOneAsync(x => x.Id == policy.Id, updateDefinition);

        // Cập nhật lại trong cache
        await UpdateRedisCacheAsync(policy);
    }

    public async Task SwitchToLatestVersionAsync()
    {
        // Lấy bản đang active
        RoyaltyPolicy activePolicy = await _unitOfWork.GetCollection<RoyaltyPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("No active royalty policy found to disable.");

        // Lấy version cao nhất
        RoyaltyPolicy newestPolicy = await _unitOfWork.GetCollection<RoyaltyPolicy>()
            .Find(_ => true)
            .SortByDescending(x => x.Version)
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("No royalty policies found in system.");

        // Kiểm tra: nếu bản active đã là bản mới nhất thì không được disable
        if (activePolicy.Version >= newestPolicy.Version)
        {
            throw new BadRequestCustomException("Cannot disable the latest active version.");
        }

        // Bản mới nhất phải ở trạng thái Pending để có thể kích hoạt
        if (newestPolicy.Status != PolicyStatus.Pending)
        {
            throw new BadRequestCustomException("The latest version is not in a pending state, cannot activate.");
        }

        // Transaction: disable bản hiện tại + active bản mới nhất
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            UpdateDefinitionBuilder<RoyaltyPolicy> update = Builders<RoyaltyPolicy>.Update;

            // Disable bản active hiện tại
            await _unitOfWork.GetCollection<RoyaltyPolicy>()
                .UpdateOneAsync(session, x => x.Id == activePolicy.Id, update.Set(x => x.Status, PolicyStatus.Inactive));

            // Enable bản mới nhất
            await _unitOfWork.GetCollection<RoyaltyPolicy>()
                .UpdateOneAsync(session, x => x.Id == newestPolicy.Id, update.Set(x => x.Status, PolicyStatus.Active));
        });

        // Cập nhật lại cache trong Redis
        await UpdateRedisCacheAsync(newestPolicy);

        // Gửi email thông báo tới người dùng về chính sách mới
        string content = $@"
            <ul style=""padding-left: 20px; margin: 0;"">
              <li><strong>Rate Per Stream:</strong> {newestPolicy.RatePerStream} vnd</li>
              <li><strong>Recording Percentage:</strong> {newestPolicy.RecordingPercentage}%</li>
              <li><strong>Work Percentage:</strong> {newestPolicy.WorkPercentage}%</li>
            </ul>
        ";

        foreach (User? user in await _unitOfWork.GetCollection<User>()
            .Find(x => x.Role == UserRole.Artist)
            .Project<User>(Builders<User>.Projection
                .Include(x => x.Id)
                .Include(x => x.Email)
                .Include(x => x.FullName))
            .ToListAsync())
        {
            BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.UpdatePolicy, user.Email, user.FullName, user.Email, content));
        }
    }

    // Method for initializing policy when the system is first set up
    public async Task InitializePolicyAsync()
    {
        RoyaltyPolicy currentPolicy = await _unitOfWork.GetCollection<RoyaltyPolicy>().Find(x => x.Status == PolicyStatus.Active).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any royal policy is active");

        await UpdateRedisCacheAsync(currentPolicy);
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

        await InitializePolicyAsync();
    }

    private async Task UpdateRedisCacheAsync(RoyaltyPolicy policy)
    {
        Dictionary<string, string?> policyDictionary = new()
        {
            { "rate_per_stream", policy.RatePerStream.ToString() },
            { "currency", policy.Currency.ToString() },
            { "recording_percentage", policy.RecordingPercentage.ToString() },
            { "work_percentage", policy.WorkPercentage.ToString() },
            { "version", policy.Version.ToString() },
            { "status", policy.Status.ToString() }
        };

        await _redisCacheService.HashSetAsync("royalty_policy:active", policyDictionary);
    }
}
