using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.BillingPortalConfigurations;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Stripe;
using Stripe.BillingPortal;

namespace EkofyApp.Infrastructure.Services.BillingPortalConfigurations;
public sealed class BillingPortalConfigurationService(IUnitOfWork unitOfWork, ILogger<BillingPortalConfiguration> logger) : IBillingPortalConfigurationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<BillingPortalConfiguration> _logger = logger;

    // Cần cải thiện thêm cho FE có thể lấy được product ids để cấu hình billing portal
    public async Task CreateBillingPortalConfiguration(CreateBillingPortalConfigurationRequest createBillingPortalConfigurationRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            try
            {
                // Bước này giúp cho fe có thể lấy product ids để cấu hình billing portal
                // Nhưng hiện tại chưa cần thiết
                //// Lấy product ids từ DB
                //string subscriptionId = await _unitOfWork.GetCollection<Subscription>()
                //    .Find(x => x.Tier == createBillingPortalConfigurationRequest.SubscriptionTier &&
                //    x.Version == createBillingPortalConfigurationRequest.SubscriptionVersion &&
                //    x.Status == SubscriptionStatus.Active)
                //    .Project(x => x.Id)
                //    .FirstOrDefaultAsync();

                //SubscriptionPlan subscriptionPlan = await _unitOfWork.GetCollection<SubscriptionPlan>()
                //    .Find(x => x.SubscriptionId == subscriptionId)
                //    .FirstOrDefaultAsync();

                // Cấu hình Billing Portal trên Stripe
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
                        InvoiceHistory = new ConfigurationFeaturesInvoiceHistoryOptions
                        {
                            Enabled = createBillingPortalConfigurationRequest.InvoiceHistoryEnabled,
                        }
                    }
                };
                ConfigurationService configService = new();
                Configuration configuration = configService.Create(options);

                // Kiểm tra xem đã tồn tại nếu rồi thì oke
                // Workaround vì lười làm webhook
                // Cũng không hẳn vì webhook setup khá tốn nhiều bước
                // Để tiện cho việc demo và test thì workaround vậy
                Configuration existedConfiguration = configService.Get(configuration.Id);
                if (existedConfiguration == null)
                {
                    throw new UnprocessableEntityCustomException($"BillingPortalConfiguration Id with {configuration.Id} does not exist.");
                }

                await _unitOfWork.GetCollection<BillingPortalConfiguration>().InsertOneAsync(new BillingPortalConfiguration
                {
                    StripeBillingPortalConfigurationId = configuration.Id,

                    UserRole = createBillingPortalConfigurationRequest.UserRole,
                    Version = createBillingPortalConfigurationRequest.Version,

                    CustomerUpdateEnabled = createBillingPortalConfigurationRequest.CustomerUpdateEnabled,
                    AllowedCustomerUpdates = createBillingPortalConfigurationRequest.AllowedCustomerUpdates,

                    PaymentMethodUpdateEnabled = createBillingPortalConfigurationRequest.PaymentMethodUpdateEnabled,

                    InvoiceHistoryEnabled = createBillingPortalConfigurationRequest.InvoiceHistoryEnabled,

                    SubscriptionCancelEnabled = createBillingPortalConfigurationRequest.SubscriptionCancelEnabled,
                    CancelMode = createBillingPortalConfigurationRequest.Mode,

                    SuscriptionUpdateEnabled = createBillingPortalConfigurationRequest.SuscriptionUpdateEnabled,
                    AllowedSubscriptionUpdates = createBillingPortalConfigurationRequest.AllowedSubscriptionUpdates,
                    Products = createBillingPortalConfigurationRequest.Products.Select(productRequest => new StripeProduct
                    {
                        Id = productRequest.Id,
                        StripePriceIds = productRequest.StripePriceIds
                    }).ToList(),

                    Status = createBillingPortalConfigurationRequest.Status,
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
