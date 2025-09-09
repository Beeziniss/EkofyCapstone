using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Stripe;

namespace EkofyApp.Infrastructure.Services.Subscriptions;
public sealed class SubscriptionService(IUnitOfWork unitOfWork, ILogger<SubscriptionService> logger) : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<SubscriptionService> _logger = logger;

    public IQueryable<Subscription> GetSubscriptions()
    {
        return _unitOfWork.GetCollection<Subscription>().AsQueryable();
    }

    public async Task CreateSubscriptionAsync(CreateSubscriptionRequest createSubscriptionRequest)
    {
        await _unitOfWork.GetCollection<Subscription>().InsertOneAsync(new Subscription
        {
            Name = createSubscriptionRequest.Name,
            Description = createSubscriptionRequest.Description,
            Code = createSubscriptionRequest.Code,
            Version = createSubscriptionRequest.Version,
            Amount = createSubscriptionRequest.Price,
            Tier = createSubscriptionRequest.Tier,
            Status = createSubscriptionRequest.Status,
            Entitlements = createSubscriptionRequest.Entitlements.Select(f => new Entitlement
            {
                Name = f.Name,
                Code = f.Code,
                Description = f.Description,
                ValueType = f.ValueType,
                Value = f.Value,
                ExpiredAt = f.ExpiredAt
            }).ToList()
        });
    }

    public async Task UpdateMetadataSubscriptionAsync(UpdateMetdataSubscriptionRequest updateMetadataSubscriptionRequest)
    {
        List<UpdateDefinition<Subscription>> updateDefinitions = [];
        UpdateDefinitionBuilder<Subscription> updateBuilder = Builders<Subscription>.Update;

        if (updateMetadataSubscriptionRequest.Name != null)
        {
            updateDefinitions.Add(updateBuilder.Set(x => x.Name, updateMetadataSubscriptionRequest.Name));
        }
        if (updateMetadataSubscriptionRequest.Description != null)
        {
            updateDefinitions.Add(updateBuilder.Set(x => x.Description, updateMetadataSubscriptionRequest.Description));
        }
        if (updateMetadataSubscriptionRequest.Code != null)
        {
            updateDefinitions.Add(updateBuilder.Set(x => x.Code, updateMetadataSubscriptionRequest.Code));
        }
        if (updateMetadataSubscriptionRequest.Amount > 0 || updateMetadataSubscriptionRequest.Amount != null)
        {
            updateDefinitions.Add(updateBuilder.Set(x => x.Amount, updateMetadataSubscriptionRequest.Amount));
        }
        if (updateMetadataSubscriptionRequest.Currency != null)
        {
            updateDefinitions.Add(updateBuilder.Set(x => x.Currency, updateMetadataSubscriptionRequest.Currency));
        }

        UpdateDefinition<Subscription> updateDefinition = updateBuilder.Combine(updateDefinitions);

        UpdateResult updateResult = await _unitOfWork.GetCollection<Subscription>()
        .UpdateOneAsync(x => x.Id == updateMetadataSubscriptionRequest.SubscriptionId, updateDefinition);
        if (updateResult.MatchedCount == 0)
        {
            throw new NotFoundCustomException("Subscription not found.");
}
        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Cannot update this subscription.");
        }
    }

    //public async Task UpdateEntitlementsSubscriptionAsync(UpdateEntitlementsSubscriptionRequest updateEntitlementsSubscriptionRequest)
    //{
    //    UpdateResult updateResult = await _unitOfWork.GetCollection<Subscription>()
    //    .UpdateOneAsync(x => x.Id == updateEntitlementsSubscriptionRequest.SubscriptionId,
    //        Builders<Subscription>.Update.Set(x => x.Entitlements, updateEntitlementsSubscriptionRequest.Entitlements.Select(f => new Entitlement
    //        { 
    //            Name = f.Name,
    //            Code = f.Code,
    //            Description = f.Description,
    //            ValueType = f.ValueType,
    //            Value = f.Value,
    //            ExpiredAt = f.ExpiredAt
    //        }).ToList()));
    //    if (updateResult.MatchedCount == 0)
    //    {
    //        throw new NotFoundCustomException("Subscription not found.");
    //    }
    //    if (updateResult.ModifiedCount == 0)
    //    {
    //        throw new UnprocessableEntityCustomException("Cannot update this subscription.");
    //    }
    //}

    public async Task DeprecateSubscriptionAsync(string subscriptionId)
{
    UpdateResult updateResult = await _unitOfWork.GetCollection<Subscription>()
        .UpdateOneAsync(x => x.Id == subscriptionId, Builders<Subscription>.Update.Set(x => x.Status, SubscriptionStatus.Deprecated));

    if (updateResult.MatchedCount == 0)
    {
        throw new NotFoundCustomException("Subscription not found.");
    }

    if (updateResult.ModifiedCount == 0)
    {
        throw new UnprocessableEntityCustomException("Cannot deprecate this subscription.");
    }
}

public async Task CreateSubscriptionPlanAsync(CreateSubScriptionPlanRequest createSubScriptionPlanRequest)
{
    await _unitOfWork.ExecuteInTransactionAsync(async session =>
    {
        try
        {
            // Kiểm tra có subscription trươcc khi tạo
            Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
                .Find(x => x.Tier == createSubScriptionPlanRequest.SubscriptionTier &&
                    x.Version == createSubScriptionPlanRequest.SubscriptionVersion &&
                    x.Status == SubscriptionStatus.Active)
                .Project<Subscription>(Builders<Subscription>.Projection
                    .Include(x => x.Id)
                    .Include(x => x.Amount))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any subscription. Please create subscription first.");

            createSubScriptionPlanRequest.Metadata ??= [];
            if (createSubScriptionPlanRequest.Metadata.TryGetValue("name", out _))
            {
                throw new BadRequestCustomException("Metadata's key input must not contain 'name' key.");
            }
            createSubScriptionPlanRequest.Metadata.Add("name", createSubScriptionPlanRequest.Name);
            createSubScriptionPlanRequest.Metadata.Add("subscription_id", subscription.Id);
            createSubScriptionPlanRequest.Metadata.Add("subscription_tier", createSubScriptionPlanRequest.SubscriptionTier.ToString());
            createSubScriptionPlanRequest.Metadata.Add("subscription_version", createSubScriptionPlanRequest.SubscriptionVersion.ToString());

            PriceService priceService = new();
            ProductService productService = new();

            StripeList<Price> existingPrices = priceService.List(new PriceListOptions
            {
                LookupKeys = createSubScriptionPlanRequest.Prices.Select(x => x.LookupKey).ToList(),
                Limit = 10
            });

            // Kiểm tra nếu đã tồn tại Amount với lookup_key này
            if (existingPrices.Data.Count > 0)
            {
                throw new ConflictCustomException("Amount with the same lookup_key already exists.");
            }

            StripeSearchResult<Product> existingProducts = await productService.SearchAsync(new ProductSearchOptions
            {
                Query = $"active:'true' AND metadata['name']:'{createSubScriptionPlanRequest.Name}'",
                Limit = 1
            });
            if (existingProducts.Data.Count > 0)
            {
                throw new ConflictCustomException("Product with the same name already exists.");
            }

            // Tạo Product mới
            Product product = await productService.CreateAsync(new ProductCreateOptions
            {
                Active = true,
                Name = createSubScriptionPlanRequest.Name,
                // Tùy chọn thêm metadata và cách thay thế lookup_key
                Metadata = createSubScriptionPlanRequest.Metadata,
                Images = createSubScriptionPlanRequest.Images,
                Type = "service",
            });

            // Tạo Amount với lookup_key
            foreach (CreatePriceRequest createPriceRequest in createSubScriptionPlanRequest.Prices)
            {
                decimal actualPrice = createPriceRequest.Interval switch
                {
                    PeriodTime.day => (subscription.Amount * 12) / 365,
                    PeriodTime.month => subscription.Amount,             // Giá gốc là tháng
                    PeriodTime.week => (subscription.Amount * 12) / 52,
                    PeriodTime.year => subscription.Amount * 12,
                    _ => throw new BadRequestCustomException("Invalid interval. Supported values are 'day', 'week', 'month' and 'year'.")
                };

                Price price = await priceService.CreateAsync(new PriceCreateOptions
                {
                    Active = true,
                    UnitAmountDecimal = actualPrice,
                    Currency = CurrencyType.vnd.ToString(),
                    Recurring = new PriceRecurringOptions
                    {
                        Interval = createPriceRequest.Interval.ToString(),              // chu kỳ
                        IntervalCount = createPriceRequest.IntervalCount,              // n chu kỳ một lần thanh toán
                    },
                    Product = product.Id,
                    LookupKey = createPriceRequest.LookupKey,
                    // Tùy chọn thêm metadata và cách thay thế lookup_key
                    //Metadata = new Dictionary<string, string>
                    //{
                    //    { "plan_type", "premium_monthly" }
                    //}
                });
            }

            // Kiểm tra lại các Amount đã tạo
            PriceListOptions options = new()
            {
                LookupKeys = createSubScriptionPlanRequest.Prices.Select(x => x.LookupKey).ToList(),
                Limit = createSubScriptionPlanRequest.Prices.Count
            };
            PriceService service = new();
            StripeList<Price> prices = service.List(options);
            if (prices.Data.Count != createSubScriptionPlanRequest.Prices.Count)
            {
                throw new UnprocessableEntityCustomException("Cannot create all prices for this subscription plan.");
            }

            await _unitOfWork.GetCollection<SubscriptionPlan>().InsertOneAsync(new SubscriptionPlan
            {
                SubscriptionId = subscription.Id,
                StripeProductId = product.Id,
                StripeProductActive = product.Active,
                StripeProductName = product.Name,
                StripeProductImages = product.Images,
                StripeProductType = product.Type,
                StripeProductMetadata = product.Metadata.Select(x => new Metadata { Key = x.Key, Value = x.Value }).ToList(),
                SubscriptionPlanPrices = prices.Data.Select(price => new SubscriptionPlanPrice
                {
                    StripePriceId = price.Id,
                    StripePriceActive = price.Active,
                    StripePriceUnitAmount = price.UnitAmount ?? 0,
                    StripePriceCurrency = price.Currency,

                    StripePriceLookupKey = price.LookupKey,
                    StripePriceMetadata = price.Metadata.Select(x => new Metadata { Key = x.Key, Value = x.Value }).ToList(),

                    Interval = Enum.Parse<PeriodTime>(price.Recurring.Interval),
                    IntervalCount = price.Recurring.IntervalCount
                }).ToList()
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error while creating subscription plan.");
            throw new UnprocessableEntityCustomException("Cannot create SubscriptionPlan");
        }
    });
}
}
