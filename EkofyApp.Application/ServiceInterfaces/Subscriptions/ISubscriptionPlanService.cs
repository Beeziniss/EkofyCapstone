using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Subscriptions;
public interface ISubscriptionPlanService
{
    IQueryable<SubscriptionPlan> GetSubscriptionPlans();
}
