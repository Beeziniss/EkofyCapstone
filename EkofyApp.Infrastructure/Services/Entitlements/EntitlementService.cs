using EkofyApp.Application.Models.Entitlements;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Entitlements;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Entitlements;
public sealed class EntitlementService(IUnitOfWork unitOfWork) : IEntitlementService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Entitlement> GetEntitlements()
    {
        return _unitOfWork.GetCollection<Entitlement>().AsQueryable();
    }

    public async Task CreateEntitlementAsync(CreateEntitlementRequest createEntitlementRequest)
    {
        await _unitOfWork.GetCollection<Entitlement>().InsertOneAsync(new Entitlement
        {
            Code = createEntitlementRequest.Code,
            Name = createEntitlementRequest.Name,
            Description = createEntitlementRequest.Description,
            ValueType = createEntitlementRequest.ValueType,
            DefaultValues = createEntitlementRequest.DefaultValues
                .Select(x => new EntitlementRoleDefault { Role = x.Role, Value = x.Value })
                .ToList(),
            SubscriptionOverrides = createEntitlementRequest.SubscriptionOverrides
                .Select(x => new EntitlementSubscriptionOverride { SubscriptionCode = x.SubscriptionCode, Value = x.Value })
                .ToList(),
            IsActive = createEntitlementRequest.IsActive
        });
    }

    public async Task<long> GetEntitlementUserCount(string code)
    {
        return await _unitOfWork.GetCollection<EffectiveEntitlement>()
            .Find(ef => ef.Entitlements.Any(e => e.Code == code))
            .CountDocumentsAsync();
    }

    public async Task DeactiveEntitlementAsync(string code)
    {
        // Kiểm tra xem có người dùng nào đang sử dụng entitlement này không
        bool hasUsers = await _unitOfWork.GetCollection<EffectiveEntitlement>()
            .Find(ef => ef.Entitlements.Any(e => e.Code == code))
            .AnyAsync();

        if (hasUsers)
        {
            throw new ConflictCustomException($"Cannot deactivate entitlement '{code}' because users are currently using it or does not exist.");
        }

        await _unitOfWork.GetCollection<Entitlement>().UpdateOneAsync(e => e.Code == code, Builders<Entitlement>.Update.Set(x => x.IsActive, false));
    }

    public async Task ReactiveEntitlementAsync(string code)
    {
        // Chỉ những entitlement đã bị deactivate mới có thể kích hoạt lại
        bool isInactive = await _unitOfWork.GetCollection<Entitlement>()
            .Find(e => e.Code == code && e.IsActive == false)
            .AnyAsync();

        if (!isInactive)
        {
            throw new ConflictCustomException($"Entitlement '{code}' is already active or does not exist.");
        }

        await _unitOfWork.GetCollection<Entitlement>().UpdateOneAsync(e => e.Code == code, Builders<Entitlement>.Update.Set(x => x.IsActive, true));
    }

    public async Task SeedDataAsync()
    {
        await _unitOfWork.GetCollection<Entitlement>().InsertManyAsync(
        [
            new()
            {
                Code = "audio_high_quality",
                Name = "High Quality Audio",
                Description = "Access to high quality audio playback",
                ValueType = EntitlementValueType.String,
                DefaultValues =
                [
                    new() { Role = UserRole.Listener, Value = "128kbps" },
                    new() { Role = UserRole.Artist, Value = "320kbps" },
                ],
                SubscriptionOverrides =
                [
                    new() { SubscriptionCode = "listener_premium", Value = "320kbps" }
                ],
                IsActive = true
            },
            new()
            {
                Code = "download_offline",
                Name = "Offline Downloads",
                Description = "Download tracks for offline playback",
                ValueType = EntitlementValueType.Boolean,
                DefaultValues =
                [
                    new() { Role = UserRole.Listener, Value = false },
                    new() { Role = UserRole.Artist, Value = true },
                ],
                SubscriptionOverrides =
                [
                    new() { SubscriptionCode = "listener_premium", Value = true }
                ],
                IsActive = false
            },
            new()
            {
                Code = "ad_free_experience",
                Name = "Ad-Free Experience",
                Description = "Enjoy music without advertisements",
                ValueType = EntitlementValueType.Boolean,
                DefaultValues =
                [
                    new() { Role = UserRole.Listener, Value = false },
                    new() { Role = UserRole.Artist, Value = true },
                ],
                SubscriptionOverrides =
                [
                    new() { SubscriptionCode = "listener_premium", Value = true }
                ],
                IsActive = false
            },
            new()
            {
                Code = "track_skip_limit",
                Name = "Track Skip Limit",
                Description = "Number of tracks that can be skipped per session",
                ValueType = EntitlementValueType.String,
                DefaultValues =
                [
                    new() { Role = UserRole.Listener, Value = "5" },
                    new() { Role = UserRole.Artist, Value = "Unlimited" },
                ],
                SubscriptionOverrides =
                [
                    new() { SubscriptionCode = "listener_premium", Value = "Unlimited" }
                ],
                IsActive = true
            },
            new()
            {
                Code = "analytics_basic",
                Name = "Basic Analytics",
                Description = "Access to basic listener statistics and insights",
                ValueType = EntitlementValueType.Boolean,
                DefaultValues =
                [
                    new() { Role = UserRole.Artist, Value = true }
                ],
                SubscriptionOverrides = [],
                IsActive = true
            },
            new()
            {
                Code = "analytics_advanced",
                Name = "Advanced Analytics",
                Description = "Access to advanced listener and playback insights",
                ValueType = EntitlementValueType.Boolean,
                DefaultValues =
                [
                    new() { Role = UserRole.Artist, Value = false }
                ],
                SubscriptionOverrides =
                [
                    new() { SubscriptionCode = "artist_pro", Value = true }
                ],
                IsActive = true
            },
            new()
            {
                Code = "priority_support",
                Name = "Priority Support",
                Description = "Access to priority customer support",
                ValueType = EntitlementValueType.Boolean,
                DefaultValues =
                [
                    new() { Role = UserRole.Listener, Value = false },
                    new() { Role = UserRole.Artist, Value = false }
                ],
                SubscriptionOverrides =
                [
                    new() { SubscriptionCode = "listener_premium", Value = true },
                    new() { SubscriptionCode = "artist_pro", Value = true }
                ],
                IsActive = true
            },
            new()
            {
                Code = "audio_file_search",
                Name = "Audio File Search",
                Description = "Search for tracks using audio file recognition",
                ValueType = EntitlementValueType.Boolean,
                DefaultValues =
                [
                    new() { Role = UserRole.Listener, Value = false },
                    new() { Role = UserRole.Artist, Value = true }
                ],
                SubscriptionOverrides = 
                [
                    new() { SubscriptionCode = "listener_premium", Value = true },
                ],
                IsActive = true
            },
            new()
            {
                Code = "period_time_recommendations",
                Name = "Period Time Track Recommendations",
                Description = "Receive personalized period time track suggestions",
                ValueType = EntitlementValueType.Boolean,
                DefaultValues =
                [
                    new() { Role = UserRole.Listener, Value = "week" },
                    new() { Role = UserRole.Artist, Value = "day" }
                ],
                SubscriptionOverrides =
                [
                    new() { SubscriptionCode = "listener_premium", Value = "day" }
                ],
                IsActive = true
            },
            new()
            {
                Code = "semantic_search",
                Name = "Semantic Search",
                Description = "Advanced search using natural language and semantic understanding",
                ValueType = EntitlementValueType.Boolean,
                DefaultValues =
                [
                    new() { Role = UserRole.Listener, Value = false },
                    new() { Role = UserRole.Artist, Value = true }
                ],
                SubscriptionOverrides = 
                [
                    new() { SubscriptionCode = "listener_premium", Value = true },
                ],
                IsActive = true
            }
        ]);
    }
}
