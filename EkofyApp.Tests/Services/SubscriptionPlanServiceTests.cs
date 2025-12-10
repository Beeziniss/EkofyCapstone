using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Infrastructure.Services.Subscriptions;
using EkofyApp.Tests.Helpers;
using MongoDB.Driver;

namespace EkofyApp.Tests.Services;

public class SubscriptionPlanServiceTests : BaseServiceTest
{
    private readonly SubscriptionPlanService _subscriptionPlanService;

    public SubscriptionPlanServiceTests()
    {
        _subscriptionPlanService = new SubscriptionPlanService(
            MockUnitOfWork.Object
        );
    }

    [Fact]
    public void GetSubscriptionPlans_ShouldReturnQueryableOfSubscriptionPlans()
    {
        // Arrange
        var subscriptionPlans = new List<SubscriptionPlan>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SubscriptionId = Guid.NewGuid().ToString(),
                StripeProductId = "prod_test1",
                StripeProductName = "Basic Plan"
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                SubscriptionId = Guid.NewGuid().ToString(),
                StripeProductId = "prod_test2",
                StripeProductName = "Premium Plan"
            }
        };
        SetupMockCollection(subscriptionPlans);

        // Act
        var result = _subscriptionPlanService.GetSubscriptionPlans();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        var plansList = result.ToList();
        plansList[0].StripeProductName.Should().Be("Basic Plan");
        plansList[1].StripeProductName.Should().Be("Premium Plan");
    }

    [Fact]
    public void GetSubscriptionPlans_WhenEmpty_ShouldReturnEmptyQueryable()
    {
        // Arrange
        SetupMockCollection<SubscriptionPlan>(); // Empty collection

        // Act
        var result = _subscriptionPlanService.GetSubscriptionPlans();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSubscriptionPlans_ShouldReturnCorrectProperties()
    {
        // Arrange
        var subscriptionPlanId = Guid.NewGuid().ToString();
        var subscriptionId = Guid.NewGuid().ToString();
        
        var subscriptionPlan = new SubscriptionPlan
        {
            Id = subscriptionPlanId,
            SubscriptionId = subscriptionId,
            StripeProductId = "prod_test",
            StripeProductActive = true,
            StripeProductName = "Test Plan",
            StripeProductType = "service",
            SubscriptionPlanPrices = new List<SubscriptionPlanPrice>
            {
                new()
                {
                    StripePriceId = "price_test",
                    StripePriceActive = true,
                    StripePriceUnitAmount = 1999,
                    StripePriceCurrency = "usd",
                    Interval = PeriodTime.month,
                    IntervalCount = 1
                }
            }
        };

        SetupMockCollection(new List<SubscriptionPlan> { subscriptionPlan });

        // Act
        var result = _subscriptionPlanService.GetSubscriptionPlans();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        
        var plan = result.First();
        plan.Id.Should().Be(subscriptionPlanId);
        plan.SubscriptionId.Should().Be(subscriptionId);
        plan.StripeProductId.Should().Be("prod_test");
        plan.StripeProductActive.Should().BeTrue();
        plan.StripeProductName.Should().Be("Test Plan");
        plan.StripeProductType.Should().Be("service");
        plan.SubscriptionPlanPrices.Should().HaveCount(1);
        
        var price = plan.SubscriptionPlanPrices.First();
        price.StripePriceId.Should().Be("price_test");
        price.StripePriceActive.Should().BeTrue();
        price.StripePriceUnitAmount.Should().Be(1999);
        price.StripePriceCurrency.Should().Be("usd");
        price.Interval.Should().Be(PeriodTime.month);
        price.IntervalCount.Should().Be(1);
    }

    [Fact]
    public void GetSubscriptionPlans_WithMultiplePrices_ShouldReturnAllPrices()
    {
        // Arrange
        var subscriptionPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid().ToString(),
            SubscriptionId = Guid.NewGuid().ToString(),
            StripeProductId = "prod_test",
            StripeProductName = "Multi-Price Plan",
            SubscriptionPlanPrices = new List<SubscriptionPlanPrice>
            {
                new()
                {
                    StripePriceId = "price_monthly",
                    Interval = PeriodTime.month,
                    IntervalCount = 1,
                    StripePriceUnitAmount = 1999
                },
                new()
                {
                    StripePriceId = "price_yearly", 
                    Interval = PeriodTime.year,
                    IntervalCount = 1,
                    StripePriceUnitAmount = 19999
                }
            }
        };

        SetupMockCollection(new List<SubscriptionPlan> { subscriptionPlan });

        // Act
        var result = _subscriptionPlanService.GetSubscriptionPlans();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        
        var plan = result.First();
        plan.SubscriptionPlanPrices.Should().HaveCount(2);
        
        var monthlyPrice = plan.SubscriptionPlanPrices.First(p => p.Interval == PeriodTime.month);
        monthlyPrice.StripePriceId.Should().Be("price_monthly");
        monthlyPrice.StripePriceUnitAmount.Should().Be(1999);
        
        var yearlyPrice = plan.SubscriptionPlanPrices.First(p => p.Interval == PeriodTime.year);
        yearlyPrice.StripePriceId.Should().Be("price_yearly");
        yearlyPrice.StripePriceUnitAmount.Should().Be(19999);
    }

    [Theory]
    [InlineData(PeriodTime.day)]
    [InlineData(PeriodTime.week)]
    [InlineData(PeriodTime.month)]
    [InlineData(PeriodTime.year)]
    public void GetSubscriptionPlans_WithDifferentIntervals_ShouldReturnCorrectInterval(PeriodTime interval)
    {
        // Arrange
        var subscriptionPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid().ToString(),
            SubscriptionId = Guid.NewGuid().ToString(),
            StripeProductId = "prod_test",
            StripeProductName = $"{interval} Plan",
            SubscriptionPlanPrices = new List<SubscriptionPlanPrice>
            {
                new()
                {
                    StripePriceId = $"price_{interval}",
                    Interval = interval,
                    IntervalCount = 1,
                    StripePriceUnitAmount = 999
                }
            }
        };

        SetupMockCollection(new List<SubscriptionPlan> { subscriptionPlan });

        // Act
        var result = _subscriptionPlanService.GetSubscriptionPlans();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        
        var plan = result.First();
        var price = plan.SubscriptionPlanPrices.First();
        price.Interval.Should().Be(interval);
    }

    [Fact]
    public void GetSubscriptionPlans_WithMetadata_ShouldReturnMetadata()
    {
        // Arrange
        var subscriptionPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid().ToString(),
            SubscriptionId = Guid.NewGuid().ToString(),
            StripeProductId = "prod_test",
            StripeProductName = "Plan with Metadata",
            StripeProductMetadata = new List<Metadata>
            {
                new() { Key = "feature1", Value = "enabled" },
                new() { Key = "feature2", Value = "premium" }
            },
            SubscriptionPlanPrices = new List<SubscriptionPlanPrice>
            {
                new()
                {
                    StripePriceId = "price_test",
                    StripePriceMetadata = new List<Metadata>
                    {
                        new() { Key = "billing_cycle", Value = "monthly" }
                    },
                    Interval = PeriodTime.month,
                    IntervalCount = 1
                }
            }
        };

        SetupMockCollection(new List<SubscriptionPlan> { subscriptionPlan });

        // Act
        var result = _subscriptionPlanService.GetSubscriptionPlans();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        
        var plan = result.First();
        plan.StripeProductMetadata.Should().HaveCount(2);
        plan.StripeProductMetadata.Should().Contain(m => m.Key == "feature1" && m.Value == "enabled");
        plan.StripeProductMetadata.Should().Contain(m => m.Key == "feature2" && m.Value == "premium");
        
        var price = plan.SubscriptionPlanPrices.First();
        price.StripePriceMetadata.Should().HaveCount(1);
        price.StripePriceMetadata.Should().Contain(m => m.Key == "billing_cycle" && m.Value == "monthly");
    }
}