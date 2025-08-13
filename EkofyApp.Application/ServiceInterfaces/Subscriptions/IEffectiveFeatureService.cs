
using EkofyApp.Application.Models.Subscriptions;

namespace EkofyApp.Application.ServiceInterfaces.Subscriptions;
public interface IEffectiveFeatureService
{
    Task BuildAsync(CreateEffectiveFeatureRequest createEffectiveFeatureRequest);
    Task RebuildAsync();
}
