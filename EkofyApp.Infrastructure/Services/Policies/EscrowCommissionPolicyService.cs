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
public sealed class EscrowCommissionPolicyService(IUnitOfWork unitOfWork, IRedisCacheService redisCacheService) : IEscrowCommissionPolicyService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;

    public IQueryable<EscrowCommissionPolicy> GetEscrowCommissionPolicies()
    {
        return _unitOfWork.GetCollection<EscrowCommissionPolicy>().AsQueryable();
    }

    public async Task DowngradeVersionAsync(long? version = null)
    {
        EscrowCommissionPolicy activePolicy = await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .Project<EscrowCommissionPolicy>(Builders<EscrowCommissionPolicy>.Projection
                .Include(x => x.Id)
                .Include(x => x.Version))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("Not found any active escrow commission policy.");

        if (version.HasValue && version >= activePolicy.Version)
        {
            throw new BadRequestCustomException("The specified version must be less than the current active version.");
        }

        EscrowCommissionPolicy previousPolicy = await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(x => x.Version == (version.HasValue ? version : activePolicy.Version - 1))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("No previous version available to downgrade to.");

        // Thực hiện transaction: disable current active + enable previous
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            UpdateDefinitionBuilder<EscrowCommissionPolicy> update = Builders<EscrowCommissionPolicy>.Update;

            await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
                .UpdateOneAsync(session, x => x.Id == activePolicy.Id, update.Set(x => x.Status, PolicyStatus.Inactive));

            await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
                .UpdateOneAsync(session, x => x.Id == previousPolicy.Id, update.Set(x => x.Status, PolicyStatus.Active));
        });

        // Cập nhật lại cache
        previousPolicy.Status = PolicyStatus.Active;
        await UpdateRedisCacheAsync(previousPolicy);

        // Gửi email thông báo tới người dùng về chính sách mới
        string content = $@"
            <ul style=""padding-left: 20px; margin: 0;"">
              <li><strong>Platform fee Percentage:</strong> {previousPolicy.PlatformFeePercentage}%</li>
              <li><strong>Artist Commission Percentage:</strong> {100m - previousPolicy.PlatformFeePercentage}%</li>
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

    public async Task CreatePolicyAsync(CreateEscrowCommissionPolicyRequest createRequest)
    {
        long currentVersion = await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(_ => true)
            .SortByDescending(x => x.Version)
            .Project(x => x.Version)
            .FirstOrDefaultAsync();

        await _unitOfWork.GetCollection<EscrowCommissionPolicy>().InsertOneAsync(new()
        {
            Currency = createRequest.Currency,
            PlatformFeePercentage = createRequest.PlatformFeePercentage,
            Version = ++currentVersion,
            Status = PolicyStatus.Inactive,
        });
    }

    public async Task UpdatePolicyAsync(UpdateEscrowCommissionPolicyRequest updateRequest)
    {
        EscrowCommissionPolicy policy = await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(x => x.Version == updateRequest.Version)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException($"No pending policy found with version {updateRequest.Version}");

        List<UpdateDefinition<EscrowCommissionPolicy>> updates = [];
        UpdateDefinitionBuilder<EscrowCommissionPolicy> builder = Builders<EscrowCommissionPolicy>.Update;

        if (updateRequest.PlatformFeePercentage.HasValue)
        {
            updates.Add(builder.Set(x => x.PlatformFeePercentage, updateRequest.PlatformFeePercentage.Value));
        }

        if (updateRequest.Currency.HasValue)
        {
            updates.Add(builder.Set(x => x.Currency, updateRequest.Currency.Value));
        }

        if (updates.Count == 0)
        {
            throw new BadRequestCustomException("No valid fields provided to update.");
        }

        UpdateDefinition<EscrowCommissionPolicy> updateDefinition = builder.Combine(updates);

        await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .UpdateOneAsync(x => x.Id == policy.Id, updateDefinition);

        // Cập nhật lại trong cache
        await UpdateRedisCacheAsync(policy);
    }

    public async Task SwitchToLatestVersionAsync()
    {
        EscrowCommissionPolicy activePolicy = await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("No active escrow commission policy found.");

        EscrowCommissionPolicy newestPolicy = await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(_ => true)
            .SortByDescending(x => x.Version)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("No escrow commission policies found.");

        if (activePolicy.Version >= newestPolicy.Version)
        {
            throw new BadRequestCustomException("Cannot switch, the active version is already the latest.");
        }

        if (newestPolicy.Status != PolicyStatus.Inactive)
        {
            throw new BadRequestCustomException("The latest version is not in an inactive state, cannot activate.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            UpdateDefinitionBuilder<EscrowCommissionPolicy> update = Builders<EscrowCommissionPolicy>.Update;

            // Disable current active policy
            await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
                .UpdateOneAsync(session, x => x.Id == activePolicy.Id, update.Set(x => x.Status, PolicyStatus.Inactive));

            // Activate newest policy
            await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
                .UpdateOneAsync(session, x => x.Id == newestPolicy.Id, update.Set(x => x.Status, PolicyStatus.Active));
        });

        // Cập nhật lại cache
        newestPolicy.Status = PolicyStatus.Active;
        await UpdateRedisCacheAsync(newestPolicy);

        // Gửi email thông báo tới người dùng về chính sách mới
        string content = $@"
            <ul style=""padding-left: 20px; margin: 0;"">
              <li><strong>Platform fee Percentage:</strong> {newestPolicy.PlatformFeePercentage}%</li>
              <li><strong>Artist Commission Percentage:</strong> {100m - newestPolicy.PlatformFeePercentage}%</li>
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

    public async Task InitializePolicyAsync()
    {
        EscrowCommissionPolicy currentPolicy = await _unitOfWork.GetCollection<EscrowCommissionPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundCustomException("Not found any active escrow commission policy.");

        await UpdateRedisCacheAsync(currentPolicy);
    }

    public async Task SeedDataAsync()
    {
        await _unitOfWork.GetCollection<EscrowCommissionPolicy>().InsertOneAsync(new()
        {
            Currency = CurrencyType.vnd,
            PlatformFeePercentage = 10m,
            Version = 1,
            Status = PolicyStatus.Active,
        });

        await InitializePolicyAsync();
    }

    private async Task UpdateRedisCacheAsync(EscrowCommissionPolicy policy)
    {
        Dictionary<string, string?> policyDictionary = new()
        {
            { "currency", policy.Currency.ToString() },
            { "platform_fee_percentage", policy.PlatformFeePercentage.ToString() },
            { "version", policy.Version.ToString() },
            { "status", policy.Status.ToString() }
        };

        await _redisCacheService.HashSetAsync("escrow_commission_policy:active", policyDictionary);
    }
}
