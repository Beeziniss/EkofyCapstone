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
public sealed class EffectiveFeatureService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IEffectiveFeatureService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task BuildAsync(CreateEffectiveFeatureRequest createEffectiveFeatureRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        UserRole userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value switch
        {
            "Listener" => UserRole.Listener,
            "Artist" => UserRole.Artist,
            _ => throw new UnauthorizedCustomException("Your role is not supported.")
        };

        await _unitOfWork.GetCollection<EffectiveFeature>().InsertOneAsync(new EffectiveFeature
        {
            UserId = userId,
            Role = userRole, // Default role, can be updated later
            SubscriptionId = createEffectiveFeatureRequest.SubscriptionId, // Initially no subscription
            SubscriptionCode = createEffectiveFeatureRequest.SubscriptionCode,
            SubscriptionVersion = createEffectiveFeatureRequest.SubscriptionVersion,
            FeatureCodes = createEffectiveFeatureRequest.FeatureCodes, // No features initially
            ValidUntil = createEffectiveFeatureRequest.ValidUntil // Set to current time, will be updated later
        });
    }

    public async Task RebuildAsync(string userId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // TODO: Lookup for better performance
            UserRole userRole = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == userId)
                .Project(u => u.Role)
                .FirstOrDefaultAsync();

            UserSubscription userSubscription = await _unitOfWork.GetCollection<UserSubscription>()
                .Find(s => s.UserId == userId && s.CanceledAt != null)
                .SortByDescending(s => s.PeriodStart)
                .FirstOrDefaultAsync();

            if (userSubscription == null)
            {
                // Remove old effective features if any
                await _unitOfWork.GetCollection<EffectiveFeature>().DeleteManyAsync(f => f.UserId == userId);
                return;
            }

            Subscription subscription = await _unitOfWork.GetCollection<Subscription>().Find(s => s.Id == userSubscription.SubscriptionId).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Subscription not found.");

            EffectiveFeature effectiveFeature = new()
            {
                UserId = userId,
                Role = userRole,
                SubscriptionId = subscription.Id,
                SubscriptionCode = subscription.Code,
                SubscriptionVersion = subscription.Version,
                FeatureCodes = subscription.Features.Select(s => s.Code).ToList(),
                ValidUntil = userSubscription.PeriodEnd
            };

            await _unitOfWork.GetCollection<EffectiveFeature>().ReplaceOneAsync(ef => ef.UserId == userId, effectiveFeature);
        });
    }
}
