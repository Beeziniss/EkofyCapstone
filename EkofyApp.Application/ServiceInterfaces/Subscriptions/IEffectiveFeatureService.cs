
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Subscriptions;
public interface IEffectiveFeatureService
{
    Task BuildAsync(EffectiveFeature effectiveFeature);
    Task RebuildAsync(string userId);
}
