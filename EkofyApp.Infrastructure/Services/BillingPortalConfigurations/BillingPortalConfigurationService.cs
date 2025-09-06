using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.BillingPortalConfigurations;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.BillingPortal;

namespace EkofyApp.Infrastructure.Services.BillingPortalConfigurations;
public sealed class BillingPortalConfigurationService(IUnitOfWork unitOfWork, ILogger<BillingPortalConfiguration> logger) : IBillingPortalConfigurationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<BillingPortalConfiguration> _logger = logger;

    public async Task CreateBillingPortalConfiguration(CreateBillingPortalConfigurationRequest createBillingPortalConfigurationRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            try
            {
                ConfigurationCreateOptions options = new()
                {
                    LoginPage = new ConfigurationLoginPageOptions
                    {
                        Enabled = false,
                    },
                    Features = new ConfigurationFeaturesOptions
                    {
                        CustomerUpdate = new ConfigurationFeaturesCustomerUpdateOptions
                        {
                            Enabled = createBillingPortalConfigurationRequest.CustomerUpdateEnabled,
                            AllowedUpdates = createBillingPortalConfigurationRequest.AllowedCustomerUpdates.Select(x => x.ToString().ToLowerInvariant()).ToList(),
                        },
                        PaymentMethodUpdate = new ConfigurationFeaturesPaymentMethodUpdateOptions
                        {
                            Enabled = createBillingPortalConfigurationRequest.PaymentMethodUpdateEnabled,
                        },
                        SubscriptionCancel = new ConfigurationFeaturesSubscriptionCancelOptions()
                        {
                            Enabled = createBillingPortalConfigurationRequest.SubscriptionCancelEnabled,
                            Mode = createBillingPortalConfigurationRequest.Mode.ToString().ToLowerInvariant(),
                        },
                        SubscriptionUpdate = new ConfigurationFeaturesSubscriptionUpdateOptions
                        {
                            Enabled = createBillingPortalConfigurationRequest.SuscriptionUpdateEnabled,
                            DefaultAllowedUpdates = createBillingPortalConfigurationRequest.AllowedSubscriptionUpdates.Select(x => x.ToString().ToLowerInvariant()).ToList(),
                            Products = createBillingPortalConfigurationRequest.Products.Select(productRequest => new ConfigurationFeaturesSubscriptionUpdateProductOptions
                            {
                                Product = productRequest.Id,
                                Prices = productRequest.StripePriceIds
                            }).ToList()
                        },
                    }
                };
                ConfigurationService configService = new();
                Configuration configuration = configService.Create(options);

                await _unitOfWork.GetCollection<BillingPortalConfiguration>().InsertOneAsync(new BillingPortalConfiguration
                {
                    StripeBillingPortalConfigurationId = configuration.Id,

                    UserRole = createBillingPortalConfigurationRequest.UserRole,
                    SubscriptionTier = createBillingPortalConfigurationRequest.SubscriptionTier,
                    Version = createBillingPortalConfigurationRequest.Version,

                    CustomerUpdateEnabled = createBillingPortalConfigurationRequest.CustomerUpdateEnabled,
                    AllowedCustomerUpdates = createBillingPortalConfigurationRequest.AllowedCustomerUpdates,

                    PaymentMethodUpdateEnabled = createBillingPortalConfigurationRequest.PaymentMethodUpdateEnabled,

                    InvoiceHistoryEnabled = createBillingPortalConfigurationRequest.InvoiceHistoryEnabled,

                    SubscriptionCancelEnabled = createBillingPortalConfigurationRequest.SubscriptionCancelEnabled,
                    Mode = createBillingPortalConfigurationRequest.Mode,

                    SuscriptionUpdateEnabled = createBillingPortalConfigurationRequest.SuscriptionUpdateEnabled,
                    AllowedSubscriptionUpdates = createBillingPortalConfigurationRequest.AllowedSubscriptionUpdates,
                    Products = createBillingPortalConfigurationRequest.Products.Select(productRequest => new StripeProduct
                    {
                        Id = productRequest.Id,
                        StripePriceIds = productRequest.StripePriceIds
                    }).ToList()
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe API error while creating billing portal configuration.");
                throw new UnprocessableEntityCustomException("Cannot create BillingPortalConfiguration");
            }
        });
    }

    public async Task UpdateBillingPortalConfiguration()
    {

    }

    public async Task DeleteBillingPortalConfiguration()
    {

    }
}
