using EkofyApp.Application.Models.Policies;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
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

    public async Task DowngradeVersionAsync(long? version = null)
    {
        LegalPolicy activePolicy = await _unitOfWork.GetCollection<LegalPolicy>()
            .Find(x => x.Status == PolicyStatus.Active)
            .Project<LegalPolicy>(Builders<LegalPolicy>.Projection
                .Include(x => x.Id)
                .Include(x => x.Version))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any legal policy is active");

        // Kiểm tra version có hợp lệ không
        if (version.HasValue && version >= activePolicy.Version)
        {
            throw new BadRequestCustomException("The specified version must be less than the current active version.");
        }

        // Tìm phiên bản trước (nếu có)
        LegalPolicy previousPolicy = await _unitOfWork.GetCollection<LegalPolicy>()
            .Find(x => x.Version == (version.HasValue ? version : activePolicy.Version - 1))
            .Project<LegalPolicy>(Builders<LegalPolicy>.Projection
                .Exclude(x => x.EffectiveAt))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("No previous version available to downgrade to.");

        // Cập nhật trạng thái của các phiên bản
        UpdateDefinition<LegalPolicy> updateActive = Builders<LegalPolicy>.Update.Set(p => p.Status, PolicyStatus.Inactive);
        UpdateDefinition<LegalPolicy> updateNext = Builders<LegalPolicy>.Update.Set(p => p.Status, PolicyStatus.Active);
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            await _unitOfWork.GetCollection<LegalPolicy>().UpdateOneAsync(session, p => p.Id == activePolicy.Id, updateActive);
            await _unitOfWork.GetCollection<LegalPolicy>().UpdateOneAsync(session, p => p.Id == previousPolicy.Id, updateNext);
        });
        // Cập nhật lại cache trong Redis
        Dictionary<string, string?> policyDictionary = new()
        {
            { "name", previousPolicy.Name },
            { "content", previousPolicy.Content },
            { "version", previousPolicy.Version.ToString() },
            { "status", previousPolicy.Status.ToString() }
        };

        await _redisCacheService.HashSetAsync("legal_policy:active", policyDictionary);
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
            Status = PolicyStatus.Inactive,
        });

        // TODO: Gửi thông báo tới người dùng (giả lập log, thực tế có thể gửi noti, email,...)

        // TODO: Lên lịch công bố chính sách sau n ngày
    }

    // Method for initializing policy when the system is first set up
    public async Task InitializePolicyAsync()
    {
        LegalPolicy currentPolicy = await _unitOfWork.GetCollection<LegalPolicy>().Find(x => x.Status == PolicyStatus.Active).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any legal policy is active");

        Dictionary<string, string?> policyDictionary = new()
        {
            { "name", currentPolicy.Name },
            { "content", currentPolicy.Content },
            { "version", currentPolicy.Version.ToString() },
            { "status", currentPolicy.Status.ToString() }
        };

        await _redisCacheService.HashSetAsync("legal_policy:active", policyDictionary);
    }

    public async Task SeedDataAsync()
    {
        await _unitOfWork.GetCollection<LegalPolicy>().InsertOneAsync(new()
        {
            Name = "Terms of Service",
            Content = "<h1>Terms of Service</h1><p>Welcome to EkofyApp! These Terms of Service govern your use of our platform. By accessing or using our services, you agree to comply with and be bound by these terms.</p>",
            Version = 1,
            Status = PolicyStatus.Active,
        });
        await _unitOfWork.GetCollection<LegalPolicy>().InsertOneAsync(new()
        {
            Name = "Privacy Policy",
            Content = "<h1>Privacy Policy</h1><p>Your privacy is important to us. This Privacy Policy explains how we collect, use, and protect your personal information when you use EkofyApp.</p>",
            Version = 1,
            Status = PolicyStatus.Active,
        });
    }
}
