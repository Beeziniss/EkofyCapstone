using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Subscriptions;
public sealed class EffectiveEntitlementService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IEffectiveEntitlementService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task BuildFreeTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, List<Entitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null)
    {
        // Hiện tại gói Free là duy nhất, không cần xet version
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Tier == SubscriptionTier.Free && x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Entitlements))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription");

        List<Entitlement> entitlementclones = new(subscription.Entitlements);
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

    public async Task BuildTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, string subscriptionId, List<Entitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null)
    {
        // Hiện tại gói Free là duy nhất, không cần xet version
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Id == subscriptionId && x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Entitlements))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found subscription");

        List<Entitlement> entitlementclones = new(subscription.Entitlements);
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

    public async Task RebuildFreeTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, List<Entitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null)
    {
        // Mặc định nếu không có subscriptionId thì sẽ lấy gói Free
        // Hiện tại gói Free là duy nhất, không cần xet version
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Tier == SubscriptionTier.Free && x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Entitlements))
            .FirstOrDefaultAsync();

        List<Entitlement> entitlementclones = new(subscription.Entitlements);
        if (additionalEntitlements != null && additionalEntitlements.Count > 0)
        {
            entitlementclones.AddRange(additionalEntitlements);
        }

        EffectiveEntitlement effectiveEntitlement = new()
        {
            UserId = userId,
            Role = userRole,
            SubscriptionId = subscription.Id,
            Entitlements = entitlementclones,
            ValidUntil = validUntil,
        };

        await _unitOfWork.GetCollection<EffectiveEntitlement>().ReplaceOneAsync(session, ef => ef.UserId == userId, effectiveEntitlement);
    }

    public async Task RebuildTierAsync(IClientSessionHandle? session, string userId, UserRole userRole, string subscriptionId, List<Entitlement>? additionalEntitlements = null, DateTimeOffset? validUntil = null)
    {
        // Mặc định nếu không có subscriptionId thì sẽ lấy gói Free
        // Hiện tại gói Free là duy nhất, không cần xet version
        Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Id == subscriptionId && x.Status == SubscriptionStatus.Active)
            .Project<Subscription>(Builders<Subscription>.Projection
                .Include(x => x.Id)
                .Include(x => x.Entitlements))
            .FirstOrDefaultAsync();

        List<Entitlement> entitlementclones = new(subscription.Entitlements);
        if (additionalEntitlements != null && additionalEntitlements.Count > 0)
        {
            entitlementclones.AddRange(additionalEntitlements);
        }

        EffectiveEntitlement effectiveEntitlement = new()
        {
            UserId = userId,
            Role = userRole,
            SubscriptionId = subscription.Id,
            Entitlements = entitlementclones,
            ValidUntil = validUntil,
        };

        await _unitOfWork.GetCollection<EffectiveEntitlement>().ReplaceOneAsync(session, ef => ef.UserId == userId, effectiveEntitlement);
    }
}
