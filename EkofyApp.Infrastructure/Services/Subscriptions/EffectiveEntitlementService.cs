using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Subscriptions;
public sealed class EffectiveEntitlementService(IUnitOfWork unitOfWork) : IEffectiveEntitlementService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task BuildFreeTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, List<AppliedEntitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null)
    {
        // Hiện tại gói Free là duy nhất, không cần xet version
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Tier == SubscriptionTier.Free && x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Code))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription");

        List<AppliedEntitlement> entitlements = await BuildEntitlementsForUserAsync(userRole, subscription.Code);

        List<AppliedEntitlement> entitlementclones = new(entitlements);
        if (additionalEntitlements != null && additionalEntitlements.Count > 0)
        {
            entitlementclones.AddRange(additionalEntitlements);
        }

        await _unitOfWork.GetCollection<EffectiveEntitlement>().InsertOneAsync(session, new EffectiveEntitlement
        {
            UserId = userId,
            Role = userRole, // Default role, can be updated later
            SubscriptionId = subscription.Id, // Initially no subscription
            Entitlements = entitlementclones,
            ValidUntil = validUntil, // Initially no expiration
        });
    }

    public async Task BuildTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, string subscriptionId, List<AppliedEntitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null)
    {
        // Hiện tại gói Free là duy nhất, không cần xet version
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Id == subscriptionId && x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Code))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription");

        List<AppliedEntitlement> entitlements = await BuildEntitlementsForUserAsync(userRole, subscription.Code);

        List<AppliedEntitlement> entitlementclones = new(entitlements);
        if (additionalEntitlements != null && additionalEntitlements.Count > 0)
        {
            entitlementclones.AddRange(additionalEntitlements);
        }

        await _unitOfWork.GetCollection<EffectiveEntitlement>().InsertOneAsync(session, new EffectiveEntitlement
        {
            UserId = userId,
            Role = userRole, // Default role, can be updated later
            SubscriptionId = subscription.Id, // Initially no subscription
            Entitlements = entitlementclones,
            ValidUntil = validUntil, // Initially no expiration
        });
    }

    public async Task RebuildFreeTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, List<AppliedEntitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null)
    {
        // Mặc định nếu không có subscriptionId thì sẽ lấy gói Free
        // Hiện tại gói Free là duy nhất, không cần xet version
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Tier == SubscriptionTier.Free && x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Code))
            .FirstOrDefaultAsync();

        List<AppliedEntitlement> entitlements = await BuildEntitlementsForUserAsync(userRole, subscription.Code);

        List<AppliedEntitlement> entitlementclones = new(entitlements);
        if (additionalEntitlements != null && additionalEntitlements.Count > 0)
        {
            entitlementclones.AddRange(additionalEntitlements);
        }

        UpdateDefinition<EffectiveEntitlement> update = Builders<EffectiveEntitlement>.Update
            .Set(x => x.Role, userRole)
            .Set(x => x.SubscriptionId, subscription.Id)
            .Set(x => x.Entitlements, entitlementclones)
            .Set(x => x.ValidUntil, validUntil)
            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        await _unitOfWork.GetCollection<EffectiveEntitlement>()
            .UpdateOneAsync(
                session,
                filter: ef => ef.UserId == userId,
                update: update,
                options: new UpdateOptions { IsUpsert = true });
    }

    public async Task RebuildTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, string subscriptionId, List<AppliedEntitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null)
    {
        // Mặc định nếu không có subscriptionId thì sẽ lấy gói Free
        // Hiện tại gói Free là duy nhất, không cần xet version
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Id == subscriptionId && x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Code))
            .FirstOrDefaultAsync();

        List<AppliedEntitlement> entitlements = await BuildEntitlementsForUserAsync(userRole, subscription.Code);

        List<AppliedEntitlement> entitlementclones = [.. entitlements];
        if (additionalEntitlements != null && additionalEntitlements.Count > 0)
        {
            entitlementclones.AddRange(additionalEntitlements);
        }

        UpdateDefinition<EffectiveEntitlement> update = Builders<EffectiveEntitlement>.Update
            .Set(x => x.Role, userRole)
            .Set(x => x.SubscriptionId, subscription.Id)
            .Set(x => x.Entitlements, entitlementclones)
            .Set(x => x.ValidUntil, validUntil)
            .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

        await _unitOfWork.GetCollection<EffectiveEntitlement>()
            .UpdateOneAsync(
                session,
                filter: x => x.UserId == userId,
                update: update,
                options: new UpdateOptions { IsUpsert = true });
    }

    private async Task<List<AppliedEntitlement>> BuildEntitlementsForUserAsync(UserRole userRole, string subscriptionCode)
    {
        List<Entitlement> entitlements = await _unitOfWork.GetCollection<Entitlement>().Find(x => x.IsActive == true).ToListAsync();
        List<AppliedEntitlement> results = [];

        foreach (Entitlement entitlement in entitlements)
        {
            object? finalValue = null;

            // Ưu tiên override theo subscription
            #region Dùng LINQ
            //EntitlementSubscriptionOverride? subOverride = entitlement.SubscriptionOverrides.FirstOrDefault(x => x.SubscriptionCode == subscriptionCode);
            //if (subOverride != null)
            //{
            //    finalValue = subOverride.Value;
            //}
            //else
            //{
            //    // Fallback sang default theo role
            //    EntitlementRoleDefault? roleDefault = entitlement.DefaultValues.FirstOrDefault(x => x.Role == userRole);
            //    if (roleDefault != null)
            //    {
            //        finalValue = roleDefault.Value;
            //    }
            //}
            #endregion

            #region Dùng Dictionary để tăng tốc độ tìm kiếm
            //Dictionary<string, object> subOverrides = entitlement.SubscriptionOverrides?.ToDictionary(x => x.SubscriptionCode, x => x.Value) ?? [];
            Dictionary<string, object> subOverrides = entitlement.SubscriptionOverrides?.Where(x => !string.IsNullOrWhiteSpace(x.SubscriptionCode))
                .ToDictionary(x => x.SubscriptionCode!, x => x.Value) ?? [];
            Dictionary<UserRole, object> roleDefaults = entitlement.DefaultValues?.ToDictionary(x => x.Role, x => x.Value) ?? [];

            if (subOverrides.TryGetValue(subscriptionCode, out object? overrideValue))
            {
                finalValue = overrideValue;
            }
            else if (roleDefaults.TryGetValue(userRole, out object? defaultValue))
            {
                finalValue = defaultValue;
            }
            #endregion

            if (finalValue != null)
            {
                results.Add(new AppliedEntitlement
                {
                    EntitlementId = entitlement.Id,
                    Code = entitlement.Code,
                    ValueType = entitlement.ValueType,
                    Value = finalValue
                });
            }
        }

        return results;
    }
}
