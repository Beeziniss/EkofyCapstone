using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Subscriptions;
public sealed class SubscriptionPlanService(IUnitOfWork unitOfWork) : ISubscriptionPlanService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<SubscriptionPlan> GetSubscriptionPlans()
    {
        return _unitOfWork.GetCollection<SubscriptionPlan>().AsQueryable();
    }
}
