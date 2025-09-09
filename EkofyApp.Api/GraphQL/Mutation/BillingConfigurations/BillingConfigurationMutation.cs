using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces.BillingPortalConfigurations;

namespace EkofyApp.Api.GraphQL.Mutation.BillingConfigurations;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class BillingConfigurationMutation(IBillingPortalConfigurationService billingPortalConfigurationService)
{
    private readonly IBillingPortalConfigurationService _billingPortalConfigurationService = billingPortalConfigurationService;
    public async Task<bool> CreateBillingPortalConfigurationAsync(CreateBillingPortalConfigurationRequest createBillingPortalConfigurationRequest)
    {
        await _billingPortalConfigurationService.CreateBillingPortalConfiguration(createBillingPortalConfigurationRequest);
        return true;
    }
}
