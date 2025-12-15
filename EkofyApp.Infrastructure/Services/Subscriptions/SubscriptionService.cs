using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Hangfire;
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
            Status = SubscriptionStatus.Inactive,
        });
    }

    public async Task ActivateSubscriptionAsync(string subscriptionId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Get the subscription to activate
            Subscription subscriptionToActivate = await _unitOfWork.GetCollection<Subscription>()
               .Find(session, x => x.Id == subscriptionId)
               .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Subscription not found.");

            // Check if subscription is already active
            if (subscriptionToActivate.Status == SubscriptionStatus.Active)
            {
                throw new ConflictCustomException("Subscription is already active.");
            }

            // Check if subscription has at least one subscription plan
            bool hasSubscriptionPlan = await _unitOfWork.GetCollection<SubscriptionPlan>()
                  .Find(session, sp => sp.SubscriptionId == subscriptionId)
                  .AnyAsync();

            if (!hasSubscriptionPlan)
            {
                throw new BadRequestCustomException("Cannot activate subscription without subscription plan. Please create subscription plan first.");
            }

            // Deactivate all other subscriptions of the same tier
            FilterDefinition<Subscription> sameTierFilter = Builders<Subscription>.Filter.And(
                Builders<Subscription>.Filter.Eq(x => x.Tier, subscriptionToActivate.Tier),
                        Builders<Subscription>.Filter.Eq(x => x.Status, SubscriptionStatus.Active)
                   );

            UpdateDefinition<Subscription> deactivateUpdate = Builders<Subscription>.Update
                    .Set(x => x.Status, SubscriptionStatus.Inactive)
                    .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

            UpdateResult deactivateResult = await _unitOfWork.GetCollection<Subscription>()
                    .UpdateManyAsync(session, sameTierFilter, deactivateUpdate);

            // Activate the target subscription
            UpdateDefinition<Subscription> activateUpdate = Builders<Subscription>.Update
                .Set(x => x.Status, SubscriptionStatus.Active)
                .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

            UpdateResult activateResult = await _unitOfWork.GetCollection<Subscription>()
                .UpdateOneAsync(session, x => x.Id == subscriptionId, activateUpdate);

            if (activateResult.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("Cannot activate this subscription.");
            }

            // Gửi email thông báo tới người dùng về chính sách mới
            UserRole userRole = subscriptionToActivate.Tier == SubscriptionTier.Premium ? UserRole.Listener : UserRole.Artist;

            string content = $@"
                <p>We would like to inform you of an upcoming <strong>update to the pricing of your subscription plan</strong>.
                This adjustment helps us continue improving our services and delivering a better experience.</p>

                <ul style=""padding-left: 20px; margin: 0;"">
                  <li>
                    <strong>Subscription Tier:</strong> {subscriptionToActivate.Tier}
                  </li>
                  <li>
                    <strong>Present Price:</strong> {subscriptionToActivate.Amount:N0} VND/month
                  </li>
                </ul>

                <p style=""margin-top: 12px;"">
                The new pricing will be applied automatically starting from the effective date.
                If you prefer not to continue your subscription at the updated price, you may cancel before this date.
                </p>

                <p>
                Thank you for your continued support and for being part of our community.
                </p>
                ";

            foreach (User? user in await _unitOfWork.GetCollection<User>()
                .Find(x => x.Role == userRole)
                .Project<User>(Builders<User>.Projection
                    .Include(x => x.Id)
                    .Include(x => x.Email)
                    .Include(x => x.FullName))
                .ToListAsync())
            {
                BackgroundJob.Enqueue<IBackgoundService>(x => x.SendEmailJob(EmailTemplateType.UpdatePolicy, user.Email, user.FullName, user.Email, content));
            }
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
                  // Kiểm tra subscription plan tồn tại thông qua Subscription Code
                  //if (await _unitOfWork.GetCollection<SubscriptionPlan>().Find(x => x..ToLowerInvariant() == createSubScriptionPlanRequest.SubscriptionCode.ToLowerInvariant() && x.Status == SubscriptionStatus.Active).AnyAsync())
                  //{
                  //    throw new ConflictCustomException("Subscription plan with the same subscription code already exists.");
                  //}

                  // Tạo subscription plan UserId
                  string subscriptionPlanId = ObjectId.GenerateNewId().ToString();

                  // 1 Subscription chỉ có duy nhất 1 SubscriptionPlan
                  // Đã validate bên FE rồi nên ko cần check nữa

                  // Kiểm tra có subscription trước khi tạo
                  Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
                 .Find(x => x.Code == createSubScriptionPlanRequest.SubscriptionCode && x.Status != SubscriptionStatus.Deprecated)
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
                          PeriodTime.year => subscription.Amount * 12,  // Giữ nguyên giá năm
                          _ => throw new BadRequestCustomException("Invalid interval. Supported values are 'day', 'week', 'month' and 'year'.")
                      };

                      long stripeActualPrice = HelperCurrencyConverter.ConvertDecimalToStripeAmount(actualPrice, CurrencyType.vnd.ToString());

                      await priceService.CreateAsync(new PriceCreateOptions
                      {
                          Active = true,
                          UnitAmountDecimal = Convert.ToDecimal(stripeActualPrice),
                          Currency = CurrencyType.vnd.ToString(),
                          Recurring = new PriceRecurringOptions
                          {
                              Interval = createPriceRequest.Interval.ToString(),       // chu kỳ
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

    public async Task UpdateSubscriptionPlanAsync(UpdateSubscriptionPlanRequest updateSubscriptionPlanRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
     {
         try
         {
             // Find the existing subscription plan
             SubscriptionPlan existingPlan = await _unitOfWork.GetCollection<SubscriptionPlan>()
                     .Find(x => x.Id == updateSubscriptionPlanRequest.SubscriptionPlanId)
                     .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Subscription plan not found.");

             // Get the subscription details for price calculation
             Subscription subscription = await _unitOfWork.GetCollection<Subscription>()
            .Find(x => x.Id == existingPlan.SubscriptionId)
          .Project<Subscription>(Builders<Subscription>.Projection
           .Include(x => x.Id)
         .Include(x => x.Amount)
           .Include(x => x.Tier)
      .Include(x => x.Version))
         .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Related subscription not found.");

             PriceService priceService = new();
             ProductService productService = new();

             // Handle adding new prices
             if (updateSubscriptionPlanRequest.NewPrices?.Count > 0)
             {
                 StripeList<Price> existingPrices = priceService.List(new PriceListOptions
                 {
                     LookupKeys = updateSubscriptionPlanRequest.NewPrices.Select(x => x.LookupKey).ToList(),
                     Limit = 50
                 });

                 if (existingPrices.Data.Count > 0)
                 {
                     throw new ConflictCustomException("One or more prices with the same lookup_key already exist.");
                 }

                 // Create new prices for the existing product
                 List<Price> newCreatedPrices = [];
                 foreach (CreatePriceRequest createPriceRequest in updateSubscriptionPlanRequest.NewPrices)
                 {
                     decimal actualPrice = createPriceRequest.Interval switch
                     {
                         PeriodTime.day => (subscription.Amount * 12) / 365,
                         PeriodTime.month => subscription.Amount,             // Giá gốc là tháng
                         PeriodTime.week => (subscription.Amount * 12) / 52,
                         PeriodTime.year => subscription.Amount * 12,        // Giá năm
                         _ => throw new BadRequestCustomException("Invalid interval. Supported values are 'day', 'week', 'month' and 'year'.")
                     };

                     long stripeActualPrice = HelperCurrencyConverter.ConvertDecimalToStripeAmount(actualPrice, CurrencyType.vnd.ToString());

                     Price newPrice = await priceService.CreateAsync(new PriceCreateOptions
                     {
                         Active = true,
                         UnitAmountDecimal = Convert.ToDecimal(stripeActualPrice),
                         Currency = CurrencyType.vnd.ToString(),
                         Recurring = new PriceRecurringOptions
                         {
                             Interval = createPriceRequest.Interval.ToString(),
                             IntervalCount = createPriceRequest.IntervalCount,
                         },
                         Product = existingPlan.StripeProductId,
                         LookupKey = createPriceRequest.LookupKey,
                         Metadata = new Dictionary<string, string>
     {
  { "subscription_version", subscription.Version.ToString() },
                { "subscription_plan_id", existingPlan.Id }
   }
                     });

                     newCreatedPrices.Add(newPrice);
                 }

                 // Add new prices to the subscription plan
                 List<SubscriptionPlanPrice> newPlanPrices = newCreatedPrices.Select(price => new SubscriptionPlanPrice
                 {
                     StripePriceId = price.Id,
                     StripePriceActive = price.Active,
                     StripePriceUnitAmount = price.UnitAmount ?? 0,
                     StripePriceCurrency = price.Currency,

                     StripePriceLookupKey = price.LookupKey,
                     StripePriceMetadata = price.Metadata.Select(x => new Metadata { Key = x.Key, Value = x.Value }).ToList(),

                     Interval = Enum.Parse<PeriodTime>(price.Recurring.Interval),
                     IntervalCount = price.Recurring.IntervalCount
                 }).ToList();

                 UpdateDefinition<SubscriptionPlan> priceUpdate = Builders<SubscriptionPlan>.Update
         .PushEach(x => x.SubscriptionPlanPrices, newPlanPrices);

                 await _unitOfWork.GetCollection<SubscriptionPlan>()
               .UpdateOneAsync(session, x => x.Id == updateSubscriptionPlanRequest.SubscriptionPlanId, priceUpdate);
             }

             // Handle updating existing prices
             if (updateSubscriptionPlanRequest.UpdatePrices?.Count > 0)
             {
                 List<UpdateDefinition<SubscriptionPlan>> priceUpdateDefinitions = [];
                 UpdateDefinitionBuilder<SubscriptionPlan> updateBuilder = Builders<SubscriptionPlan>.Update;

                 foreach (UpdatePriceRequest updatePriceRequest in updateSubscriptionPlanRequest.UpdatePrices)
                 {
                     // Find the index of the price to update
                     FilterDefinition<SubscriptionPlan> planFilter = Builders<SubscriptionPlan>.Filter.And(
           Builders<SubscriptionPlan>.Filter.Eq(x => x.Id, updateSubscriptionPlanRequest.SubscriptionPlanId),
                      Builders<SubscriptionPlan>.Filter.ElemMatch(x => x.SubscriptionPlanPrices,
                p => p.StripePriceId == updatePriceRequest.StripePriceId)
            );

                     SubscriptionPlan existingPlanWithPrice = await _unitOfWork.GetCollection<SubscriptionPlan>()
                   .Find(planFilter)
                 .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Price with ID {updatePriceRequest.StripePriceId} not found in subscription plan.");

                     // Get the index of the price in the array
                     int priceIndex = existingPlanWithPrice.SubscriptionPlanPrices
                           .FindIndex(p => p.StripePriceId == updatePriceRequest.StripePriceId);

                     if (priceIndex == -1)
                     {
                         throw new NotFoundCustomException($"Price with ID {updatePriceRequest.StripePriceId} not found in subscription plan.");
                     }

                     // Handle different update scenarios
                     bool needsNewStripePrice = updatePriceRequest.Interval.HasValue || updatePriceRequest.IntervalCount.HasValue;

                     if (needsNewStripePrice)
                     {
                         // If interval or interval count changes, we need to create a new Stripe price
                         // because these properties are immutable in Stripe
                         decimal actualPrice = updatePriceRequest.Interval?.ToString() switch
                         {
                             "day" => (subscription.Amount * 12) / 365,
                             "month" => subscription.Amount,
                             "week" => (subscription.Amount * 12) / 52,
                             "year" => subscription.Amount * 12,
                             _ => subscription.Amount // Default to monthly if not specified
                         };

                         long stripeActualPrice = HelperCurrencyConverter.ConvertDecimalToStripeAmount(actualPrice, CurrencyType.vnd.ToString());

                         // Create new price with updated properties
                         PeriodTime newInterval = updatePriceRequest.Interval ??
                               existingPlanWithPrice.SubscriptionPlanPrices[priceIndex].Interval;
                         long newIntervalCount = updatePriceRequest.IntervalCount ??
                        existingPlanWithPrice.SubscriptionPlanPrices[priceIndex].IntervalCount;

                         PriceCreateOptions priceCreateOptions = new()
                         {
                             Active = updatePriceRequest.Active ?? true,
                             UnitAmountDecimal = Convert.ToDecimal(stripeActualPrice),
                             Currency = CurrencyType.vnd.ToString(),
                             Recurring = new PriceRecurringOptions
                             {
                                 Interval = newInterval.ToString(),
                                 IntervalCount = newIntervalCount,
                             },
                             Product = existingPlan.StripeProductId,
                             LookupKey = updatePriceRequest.LookupKey ??
                           existingPlanWithPrice.SubscriptionPlanPrices[priceIndex].StripePriceLookupKey,
                             Metadata = updatePriceRequest.Metadata ?? new Dictionary<string, string>
      {
    { "subscription_version", subscription.Version.ToString() },
      { "subscription_plan_id", existingPlan.Id }
     }
                         };

                         Price newPrice = await priceService.CreateAsync(priceCreateOptions);

                         // Deactivate old price
                         await priceService.UpdateAsync(updatePriceRequest.StripePriceId, new PriceUpdateOptions
                         {
                             Active = false,
                             Metadata = new Dictionary<string, string>
        {
      { "replaced_by", newPrice.Id },
         { "replaced_at", HelperMethod.NormalizeToStringUtcPlus7(HelperMethod.GetUtcPlus7TimeOffset()) }
          }
                         });

                         // Update the subscription plan price entry
                         priceUpdateDefinitions.Add(updateBuilder.Set(
                          $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.StripePriceId)}", newPrice.Id));
                         priceUpdateDefinitions.Add(updateBuilder.Set(
                          $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.StripePriceActive)}", newPrice.Active));
                         priceUpdateDefinitions.Add(updateBuilder.Set(
                          $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.StripePriceUnitAmount)}", newPrice.UnitAmount ?? 0));
                         priceUpdateDefinitions.Add(updateBuilder.Set(
                  $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.StripePriceLookupKey)}", newPrice.LookupKey));
                         priceUpdateDefinitions.Add(updateBuilder.Set(
                    $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.StripePriceMetadata)}",
                         newPrice.Metadata.Select(x => new Metadata { Key = x.Key, Value = x.Value }).ToList()));
                         priceUpdateDefinitions.Add(updateBuilder.Set(
                             $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.Interval)}", newInterval));
                         priceUpdateDefinitions.Add(updateBuilder.Set(
                     $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.IntervalCount)}", newIntervalCount));
                     }
                     else
                     {
                         // Update only mutable properties in Stripe
                         PriceUpdateOptions priceUpdateOptions = new();
                         bool hasStripeUpdates = false;

                         if (updatePriceRequest.Active.HasValue)
                         {
                             priceUpdateOptions.Active = updatePriceRequest.Active.Value;
                             hasStripeUpdates = true;
                             priceUpdateDefinitions.Add(updateBuilder.Set(
                                  $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.StripePriceActive)}",
                                   updatePriceRequest.Active.Value));
                         }

                         if (updatePriceRequest.LookupKey != null)
                         {
                             priceUpdateOptions.LookupKey = updatePriceRequest.LookupKey;
                             hasStripeUpdates = true;
                             priceUpdateDefinitions.Add(updateBuilder.Set(
                                    $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.StripePriceLookupKey)}",
                                      updatePriceRequest.LookupKey));
                         }

                         if (updatePriceRequest.Metadata != null)
                         {
                             priceUpdateOptions.Metadata = updatePriceRequest.Metadata;
                             hasStripeUpdates = true;
                             priceUpdateDefinitions.Add(updateBuilder.Set(
                           $"{nameof(SubscriptionPlan.SubscriptionPlanPrices)}.{priceIndex}.{nameof(SubscriptionPlanPrice.StripePriceMetadata)}",
                                      updatePriceRequest.Metadata.Select(x => new Metadata { Key = x.Key, Value = x.Value }).ToList()));
                         }

                         if (hasStripeUpdates)
                         {
                             await priceService.UpdateAsync(updatePriceRequest.StripePriceId, priceUpdateOptions);
                         }
                     }
                 }

                 // Apply all price updates to the subscription plan
                 if (priceUpdateDefinitions.Count > 0)
                 {
                     UpdateDefinition<SubscriptionPlan> combinedPriceUpdate = updateBuilder.Combine(priceUpdateDefinitions);
                     await _unitOfWork.GetCollection<SubscriptionPlan>()
                      .UpdateOneAsync(session, x => x.Id == updateSubscriptionPlanRequest.SubscriptionPlanId, combinedPriceUpdate);
                 }
             }

             // Update product information if provided (same as before)
             if (!string.IsNullOrEmpty(updateSubscriptionPlanRequest.Name) ||
               updateSubscriptionPlanRequest.Images != null ||
         updateSubscriptionPlanRequest.Metadata != null)
             {
                 var productUpdateOptions = new ProductUpdateOptions();

                 if (!string.IsNullOrEmpty(updateSubscriptionPlanRequest.Name))
                 {
                     productUpdateOptions.Name = updateSubscriptionPlanRequest.Name;
                 }

                 if (updateSubscriptionPlanRequest.Images != null)
                 {
                     productUpdateOptions.Images = updateSubscriptionPlanRequest.Images;
                 }

                 if (updateSubscriptionPlanRequest.Metadata != null)
                 {
                     // Merge with existing metadata
                     var existingMetadata = existingPlan.StripeProductMetadata?.ToDictionary(x => x.Key, x => x.Value) ?? [];
                     foreach (var kvp in updateSubscriptionPlanRequest.Metadata)
                     {
                         existingMetadata[kvp.Key] = kvp.Value;
                     }
                     productUpdateOptions.Metadata = existingMetadata;
                 }

                 Product updatedProduct = await productService.UpdateAsync(existingPlan.StripeProductId, productUpdateOptions);

                 // Update the subscription plan document with new product information
                 List<UpdateDefinition<SubscriptionPlan>> updateDefinitions = [];
                 UpdateDefinitionBuilder<SubscriptionPlan> updateBuilder = Builders<SubscriptionPlan>.Update;

                 if (!string.IsNullOrEmpty(updateSubscriptionPlanRequest.Name))
                 {
                     updateDefinitions.Add(updateBuilder.Set(x => x.StripeProductName, updatedProduct.Name));
                 }

                 if (updateSubscriptionPlanRequest.Images != null)
                 {
                     updateDefinitions.Add(updateBuilder.Set(x => x.StripeProductImages, updatedProduct.Images));
                 }

                 if (updateSubscriptionPlanRequest.Metadata != null)
                 {
                     updateDefinitions.Add(updateBuilder.Set(x => x.StripeProductMetadata,
                    updatedProduct.Metadata.Select(x => new Metadata { Key = x.Key, Value = x.Value }).ToList()));
                 }

                 if (updateDefinitions.Count != 0)
                 {
                     UpdateDefinition<SubscriptionPlan> combinedUpdate = updateBuilder.Combine(updateDefinitions);
                     await _unitOfWork.GetCollection<SubscriptionPlan>()
                 .UpdateOneAsync(session, x => x.Id == updateSubscriptionPlanRequest.SubscriptionPlanId, combinedUpdate);
                 }
             }
         }
         catch (StripeException ex)
         {
             _logger.LogError(ex, "Stripe API error while updating subscription plan.");
             throw new UnprocessableEntityCustomException("Cannot update subscription plan");
         }
     });
    }
}
