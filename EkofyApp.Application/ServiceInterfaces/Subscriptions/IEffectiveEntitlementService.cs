using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Users;
using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces.Subscriptions;
public interface IEffectiveEntitlementService
{
    Task BuildFreeTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, List<AppliedEntitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null);
    Task BuildTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, string subscriptionId, List<AppliedEntitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null);
    Task<IEnumerable<string>> GetUserIdsWithPeriodTimeRecommendationsAsync();
    Task RebuildFreeTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, List<AppliedEntitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null);
    Task RebuildTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, string subscriptionId, List<AppliedEntitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null);
}
