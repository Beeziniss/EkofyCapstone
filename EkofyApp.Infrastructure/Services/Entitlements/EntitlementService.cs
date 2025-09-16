using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Entitlements;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Entitlements;
public sealed class EntitlementService(IUnitOfWork unitOfWork) : IEntitlementService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Entitlement> GetEntitlements()
    {
        return _unitOfWork.GetCollection<Entitlement>().AsQueryable();
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
                    new() { Role = UserRole.Artist, Value = "320kbps" },
                    new() { Role = UserRole.Listener, Value = "128kbps" }
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
                    new() { Role = UserRole.Artist, Value = true },
                    new() { Role = UserRole.Listener, Value = false }
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
                    new() { Role = UserRole.Artist, Value = true },
                    new() { Role = UserRole.Listener, Value = false }
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
                    new() { Role = UserRole.Artist, Value = "Unlimited" },
                    new() { Role = UserRole.Listener, Value = "5" }
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
                    new() { Role = UserRole.Listener, Value = true },
                    new() { Role = UserRole.Artist, Value = true }
                ],
                SubscriptionOverrides = [],
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
                    new() { Role = UserRole.Listener, Value = true },
                    new() { Role = UserRole.Artist, Value = true }
                ],
                SubscriptionOverrides = [],
                IsActive = true
            }
        ]);
    }
}
