using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Subscriptions;
public sealed class EffectiveFeatureService(IUnitOfWork unitOfWork) : IEffectiveFeatureService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task BuildAsync(EffectiveFeature effectiveFeature)
    {
        await _unitOfWork.GetCollection<EffectiveFeature>().InsertOneAsync(new EffectiveFeature
        {
            UserId = effectiveFeature.UserId,
            Role = effectiveFeature.Role, // Default role, can be updated later
            SubscriptionId = effectiveFeature.SubscriptionId, // Initially no subscription
            SubscriptionCode = effectiveFeature.SubscriptionCode,
            SubscriptionVersion = effectiveFeature.SubscriptionVersion,
            FeatureCodes = effectiveFeature.FeatureCodes, // No features initially
            ValidUntil = effectiveFeature.ValidUntil // Set to current time, will be updated later
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
