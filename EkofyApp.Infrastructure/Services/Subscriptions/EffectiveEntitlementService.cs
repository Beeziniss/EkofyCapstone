using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Subscriptions;
public sealed class EffectiveEntitlementService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IEffectiveEntitlementService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task BuildAsync(CreateEffectiveEntitlementRequest createEffectiveEntitlementRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        UserRole userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value switch
        {
            "Listener" => UserRole.Listener,
            "Artist" => UserRole.Artist,
            _ => throw new UnauthorizedCustomException("Your role is not supported.")
        };

        await _unitOfWork.GetCollection<EffectiveEntitlement>().InsertOneAsync(new EffectiveEntitlement
        {
            UserId = userId,
            Role = userRole, // Default role, can be updated later
            SubscriptionId = createEffectiveEntitlementRequest.SubscriptionId, // Initially no subscription
            SubscriptionCode = createEffectiveEntitlementRequest.SubscriptionCode,
            SubscriptionVersion = createEffectiveEntitlementRequest.SubscriptionVersion,
            FeatureCodes = createEffectiveEntitlementRequest.FeatureCodes, // No features initially
            ValidUntil = createEffectiveEntitlementRequest.ValidUntil // Set to current time, will be updated later
        });
    }

    public async Task RebuildAsync()
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            UserRole userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value switch
            {
                "Listener" => UserRole.Listener,
                "Artist" => UserRole.Artist,
                _ => throw new UnauthorizedCustomException("Your role is not supported.")
            };

            UserSubscription userSubscription = await _unitOfWork.GetCollection<UserSubscription>()
                .Find(s => s.UserId == userId && s.CanceledAt != null)
                .SortByDescending(s => s.PeriodStart)
                .FirstOrDefaultAsync();

            // TODO: Có nên để user Free Tier có EffectiveEntitlement không?
            // Và nếu có thì nên để FeatureCodes là empty
            // Nếu không có subscription thì xóa EffectiveEntitlement cũ
            if (userSubscription == null)
            {
                // Remove old effective features if any
                await _unitOfWork.GetCollection<EffectiveEntitlement>().DeleteManyAsync(f => f.UserId == userId);
                return;
            }

            Subscription subscription = await _unitOfWork.GetCollection<Subscription>().Find(s => s.Id == userSubscription.SubscriptionId).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Subscription not found.");

            EffectiveEntitlement effectiveEntitlement = new()
            {
                UserId = userId,
                Role = userRole,
                SubscriptionId = subscription.Id,
                SubscriptionCode = subscription.Code,
                SubscriptionVersion = subscription.Version,
                FeatureCodes = subscription.Entitlements.Select(s => s.Code).ToList(),
                ValidUntil = userSubscription.PeriodEnd
            };

            await _unitOfWork.GetCollection<EffectiveEntitlement>().ReplaceOneAsync(ef => ef.UserId == userId, effectiveEntitlement);
        });
    }
}
