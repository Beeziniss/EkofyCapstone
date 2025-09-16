using EkofyApp.Application.Models.Stripes;

namespace EkofyApp.Application.ServiceInterfaces.BillingPortalConfigurations;
public interface IBillingPortalConfigurationService
{
    Task CreateBillingPortalConfiguration(CreateBillingPortalConfigurationRequest createBillingPortalConfigurationRequest);
    Task<string> CreateCustomerPortalSessionAsync(string returnUrl, long version);
}
