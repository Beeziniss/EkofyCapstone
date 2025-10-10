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
using MongoDB.Bson;
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
        int currentVersion = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Tier == createSubscriptionRequest.Tier)
            .SortByDescending(x => x.Version)
            .Project(x => x.Version)
            .FirstOrDefaultAsync();

        await _unitOfWork.GetCollection<Subscription>().InsertOneAsync(new Subscription
        {
            Name = createSubscriptionRequest.Name,
            Description = createSubscriptionRequest.Description,
            Code = createSubscriptionRequest.Code,
            Amount = createSubscriptionRequest.Price,
            Version = ++currentVersion,
            Tier = createSubscriptionRequest.Tier,
            Status = createSubscriptionRequest.Status,
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

    // TODO: Cần kiểm tra thêm giữa list delete và add/update có trùng nhau không
    // Resolved: Tách ra làm 2 hàm riêng biệt
    //public async Task UpdateEntitlementsSubscriptionAsync(UpdateEntitlementsSubscriptionRequest updateEntitlementsSubscriptionRequest)
    //{
    //    await _unitOfWork.ExecuteInTransactionAsync(async session =>
    //    {
    //        // Thêm hoặc cập nhật entitlements nếu có
    //        if (updateEntitlementsSubscriptionRequest.Entitlements?.Any() == true)
    //        {
    //            foreach (UpdateEntitlementRequest entitlementRequest in updateEntitlementsSubscriptionRequest.Entitlements)
    //            {
    //                FilterDefinition<Subscription> filter = Builders<Subscription>.Filter.And(
    //                    Builders<Subscription>.Filter.Eq(x => x.UserId, updateEntitlementsSubscriptionRequest.SubscriptionId),
    //                    Builders<Subscription>.Filter.ElemMatch(x => x.Entitlements, e => e.Code == entitlementRequest.Code)
    //                );

    //                List<UpdateDefinition<Subscription>> updates = [];
    //                if (entitlementRequest.Name != null)
    //                {
    //                    updates.Add(Builders<Subscription>.Update.Set($"{nameof(Subscription.Entitlements)}.$.{nameof(Entitlement.Name)}", entitlementRequest.Name));
    //                }
    //                if (entitlementRequest.Description != null)
    //                {
    //                    updates.Add(Builders<Subscription>.Update.Set($"{nameof(Subscription.Entitlements)}.$.{nameof(Entitlement.Description)}", entitlementRequest.Description));
    //                }
    //                if (entitlementRequest.ValueType != default)
    //                {
    //                    updates.Add(Builders<Subscription>.Update.Set($"{nameof(Subscription.Entitlements)}.$.{nameof(Entitlement.ValueType)}", entitlementRequest.ValueType));
    //                }
    //                if (entitlementRequest.Value != null)
    //                {
    //                    updates.Add(Builders<Subscription>.Update.Set($"{nameof(Subscription.Entitlements)}.$.{nameof(Entitlement.Value)}", entitlementRequest.Value));
    //                }
    //                if (entitlementRequest.ExpiredAt != null)
    //                {
    //                    updates.Add(Builders<Subscription>.Update.Set($"{nameof(Subscription.Entitlements)}.$.{nameof(Entitlement.ExpiredAt)}", entitlementRequest.ExpiredAt));
    //                }

    //                UpdateDefinition<Subscription> combinedUpdate = Builders<Subscription>.Update.Combine(updates);
    //                UpdateResult updateResult = await _unitOfWork.GetCollection<Subscription>().UpdateOneAsync(session, filter, combinedUpdate);

    //                bool isAddToSetResult = false;
    //                if (updateResult.MatchedCount == 0)
    //                {
    //                    Entitlement newEntitlement = new()
    //                    {
    //                        Name = entitlementRequest.Name ?? throw new BadRequestCustomException("Entitlement Name is required"),
    //                        Code = entitlementRequest.Code,
    //                        Description = entitlementRequest.Description ?? throw new BadRequestCustomException("Entitlement Description is required"),
    //                        ValueType = entitlementRequest.ValueType ?? throw new BadRequestCustomException("Entitlement Value Type is required"),
    //                        Value = entitlementRequest.Value,
    //                        ExpiredAt = entitlementRequest.ExpiredAt
    //                    };

    //                    UpdateDefinition<Subscription> addToSetUpdate = Builders<Subscription>.Update.AddToSet(x => x.Entitlements, newEntitlement);
    //                    UpdateResult addToSetResult = await _unitOfWork.GetCollection<Subscription>().UpdateOneAsync(
    //                        session,
    //                        Builders<Subscription>.Filter.Eq(x => x.UserId, updateEntitlementsSubscriptionRequest.SubscriptionId),
    //                        addToSetUpdate
    //                    );

    //                    isAddToSetResult = true;
    //                    //if (addToSetResult.MatchedCount == 0)
    //                    //{
    //                    //    throw new NotFoundCustomException("Subscription not found when adding entitlement.");
    //                    //}
    //                    if (addToSetResult.ModifiedCount == 0)
    //                    {
    //                        throw new UnprocessableEntityCustomException("Cannot add entitlement to this subscription.");
    //                    }
    //                }
    //                if (updateResult.ModifiedCount == 0 && !isAddToSetResult)
    //                {
    //                    throw new UnprocessableEntityCustomException("Cannot update entitlement in this subscription.");
    //                }
    //            }
    //        }
    //    });
    //}

    //public async Task DeleteEntitlementSubsriptionAsync(DeleteEntitlementsSubscriptionRequest deleteEntitlementsSubscriptionRequest)
    //{
    //    await _unitOfWork.ExecuteInTransactionAsync(async session =>
    //    {
    //        // Xóa entitlements nếu có
    //        if (deleteEntitlementsSubscriptionRequest.Codes?.Any() == true)
    //        {
    //            UpdateResult pullResult = await _unitOfWork.GetCollection<Subscription>()
    //                .UpdateOneAsync(session,
    //                    x => x.UserId == deleteEntitlementsSubscriptionRequest.SubscriptionId,
    //                    Builders<Subscription>.Update.PullFilter(
    //                        x => x.Entitlements,
    //                        e => deleteEntitlementsSubscriptionRequest.Codes.Contains(e.Code)
    //                    )
    //                );

    //            if (pullResult.MatchedCount == 0)
    //            {
    //                throw new NotFoundCustomException("Subscription not found when removing entitlements.");
    //            }

    //            if (pullResult.ModifiedCount == 0)
    //            {
    //                throw new UnprocessableEntityCustomException("Cannot remove entitlements from this subscription.");
    //            }
    //        }
    //    });
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

    private static void ValidateKeyMetadata(params string[] keys)
    {
        if (keys.Contains("name"))
        {
            throw new BadRequestCustomException("Metadata's key input must not contain 'name' key.");
        }
        if (keys.Contains("subscription_id"))
        {
            throw new BadRequestCustomException("Metadata's key input must not contain 'subscription_id' key.");
        }
        if (keys.Contains("subscription_tier"))
        {
            throw new BadRequestCustomException("Metadata's key input must not contain 'subscription_tier' key.");
        }
        if (keys.Contains("subscription_version"))
        {
            throw new BadRequestCustomException("Metadata's key input must not contain 'subscription_version' key.");
        }
    }

    public async Task CreateSubscriptionPlanAsync(CreateSubScriptionPlanRequest createSubScriptionPlanRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            try
            {
                // Tạo subscription plan UserId
                string subscriptionPlanId = ObjectId.GenerateNewId().ToString();

                // Kiểm tra có subscription trươcc khi tạo
                Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
                    .Find(x => x.Code == createSubScriptionPlanRequest.SubscriptionCode && x.Status == SubscriptionStatus.Active)
                    .Project<Subscription>(Builders<Subscription>.Projection
                        .Include(x => x.Id)
                        .Include(x => x.Amount)
                        .Include(x => x.Tier)
                        .Include(x => x.Version))
                    .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Not found any subscription. Please create subscription first.");

                ValidateKeyMetadata(createSubScriptionPlanRequest.Metadata?.Keys.ToArray() ?? []);
                createSubScriptionPlanRequest.Metadata ??= [];
                createSubScriptionPlanRequest.Metadata.Add("name", createSubScriptionPlanRequest.Name);
                createSubScriptionPlanRequest.Metadata.Add("subscription_id", subscription.Id);
                createSubScriptionPlanRequest.Metadata.Add("subscription_tier", subscription.Tier.ToString());
                createSubScriptionPlanRequest.Metadata.Add("subscription_version", subscription.Version.ToString());

                PriceService priceService = new();
                ProductService productService = new();

                StripeList<Price> existingPrices = priceService.List(new PriceListOptions
                {
                    LookupKeys = createSubScriptionPlanRequest.Prices.Select(x => x.LookupKey).ToList(),
                    Limit = 50
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
                        PeriodTime.year => subscription.Amount * 12,        // Giữ nguyên giá năm
                        _ => throw new BadRequestCustomException("Invalid interval. Supported values are 'day', 'week', 'month' and 'year'.")
                    };

                    await priceService.CreateAsync(new PriceCreateOptions
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
                        Metadata = new Dictionary<string, string>
                        {
                            { "subscription_version", subscription.Version.ToString() },
                            { "subscription_plan_id", subscriptionPlanId }
                        }
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
                    Id = subscriptionPlanId,
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
