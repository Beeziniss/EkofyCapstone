using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces.BillingPortalConfigurations;

namespace EkofyApp.Api.GraphQL.Mutation.BillingConfigurations;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class BillingPortalConfigurationMutation(IBillingPortalConfigurationService billingPortalConfigurationService)
{
    private readonly IBillingPortalConfigurationService _billingPortalConfigurationService = billingPortalConfigurationService;

    // TODO: Cần thêm extension để config authorization
    public async Task<bool> CreateBillingPortalConfigurationAsync(CreateBillingPortalConfigurationRequest createBillingPortalConfigurationRequest)
    {
        await _billingPortalConfigurationService.CreateBillingPortalConfiguration(createBillingPortalConfigurationRequest);
        return true;
    }

    public async Task<string> CreateCustomerPortalSessionAsync(string returnUrl, long version)
    {
        return await _billingPortalConfigurationService.CreateCustomerPortalSessionAsync(returnUrl, version);
    }
}
