using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Subscriptions;
public sealed class SubscriptionService(IUnitOfWork unitOfWork) : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Subscription> GetSubscriptions()
    {
        return _unitOfWork.GetCollection<Subscription>().AsQueryable();
    }

    public async Task CreateSubscriptionAsync(CreateSubscriptionRequest createSubscriptionRequest)
    {
        await _unitOfWork.GetCollection<Subscription>().InsertOneAsync(new Subscription
        {
            Name = createSubscriptionRequest.Name,
            Description = createSubscriptionRequest.Description,
            Code = createSubscriptionRequest.Code,
            Version = createSubscriptionRequest.Version,
            Price = createSubscriptionRequest.Price,
            Tier = createSubscriptionRequest.Tier,
            Entitlements = createSubscriptionRequest.Entitlements.Select(f => new Entitlement
            {
                Name = f.Name,
                Code = f.Code,
                Description = f.Description,
                ValueType = f.ValueType,
                Value = f.Value,
                ExpiredAt = f.ExpiredAt
            }).ToList()
        });
    }
}
