
using EkofyApp.Application.Models.Subscriptions;

namespace EkofyApp.Application.ServiceInterfaces.Subscriptions;
public interface IEffectiveEntitlementService
{
    Task BuildAsync(CreateEffectiveEntitlementRequest createEffectiveFeatureRequest);
    Task RebuildAsync();
}
