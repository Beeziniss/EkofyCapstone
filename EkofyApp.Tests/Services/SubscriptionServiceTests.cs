using EkofyApp.Application.Models.Subscriptions;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Infrastructure.Services.Subscriptions;
using EkofyApp.Tests.Helpers;
using MongoDB.Driver;

namespace EkofyApp.Tests.Services;

public class SubscriptionServiceTests : BaseServiceTest
{
    private readonly SubscriptionService _subscriptionService;
    private readonly Mock<ILogger<SubscriptionService>> _mockLogger;

    public SubscriptionServiceTests()
    {
        _mockLogger = new Mock<ILogger<SubscriptionService>>();

        _subscriptionService = new SubscriptionService(
            MockUnitOfWork.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void GetSubscriptions_ShouldReturnQueryableOfSubscriptions()
    {
        // Arrange
        var subscriptions = new List<Subscription>
        {
            TestDataHelper.CreateTestSubscription(),
            TestDataHelper.CreateTestSubscription()
        };
        SetupMockCollection(subscriptions);

        // Act
        var result = _subscriptionService.GetSubscriptions();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithValidRequest_ShouldCreateSubscription()
    {
        // Arrange
        var request = new CreateSubscriptionRequest
        {
            Name = "Premium Subscription",
            Description = "Premium features subscription",
            Code = "PREMIUM",
            Price = 19.99m,
            Tier = SubscriptionTier.Premium
        };

        var mockSubscriptionCollection = SetupMockCollection<Subscription>();
        
        // Create a simple mock for the full document find result first
        var mockFindFluent = new Mock<IFindFluent<Subscription, Subscription>>();
        var mockProjectedFindFluent = new Mock<IFindFluent<Subscription, int>>();
        
        mockProjectedFindFluent.Setup(x => x.FirstOrDefaultAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(1);
        
        mockFindFluent.Setup(x => x.Project<int>(It.IsAny<ProjectionDefinition<Subscription, int>>()))
                     .Returns(mockProjectedFindFluent.Object);
        
        // Mock finding existing subscriptions for version calculation
        mockSubscriptionCollection.Setup(x => x.Find(It.IsAny<FilterDefinition<Subscription>>(), It.IsAny<FindOptions>()))
                                 .Returns(mockFindFluent.Object);

        // Act
        await _subscriptionService.CreateSubscriptionAsync(request);

        // Assert
        mockSubscriptionCollection.Verify(x => x.InsertOneAsync(
            It.Is<Subscription>(s => 
                s.Name == request.Name && 
                s.Code == request.Code && 
                s.Amount == request.Price &&
                s.Tier == request.Tier &&
                s.Version == 2 && // Should be incremented
                s.Status == SubscriptionStatus.Inactive),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_WithValidSubscription_ShouldActivateSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        var subscription = TestDataHelper.CreateTestSubscription(subscriptionId);
        subscription.Status = SubscriptionStatus.Inactive;
        subscription.Tier = SubscriptionTier.Premium;

        SetupMockCollection(new List<Subscription> { subscription });
        SetupMockCollection(new List<SubscriptionPlan> 
        { 
            new() { SubscriptionId = subscriptionId, Id = Guid.NewGuid().ToString() }
        });
        SetupSuccessfulTransaction();

        var mockSubscriptionCollection = SetupMockCollection(new List<Subscription> { subscription });
        mockSubscriptionCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<Subscription>>(),
                It.IsAny<UpdateDefinition<Subscription>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        mockSubscriptionCollection.Setup(x => x.UpdateManyAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<Subscription>>(),
                It.IsAny<UpdateDefinition<Subscription>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        await _subscriptionService.ActivateSubscriptionAsync(subscriptionId);

        // Assert
        VerifyTransactionExecuted();
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_WhenSubscriptionNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        SetupMockCollection<Subscription>(); // Empty collection
        SetupSuccessfulTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundCustomException>(() =>
            _subscriptionService.ActivateSubscriptionAsync(subscriptionId));
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_WhenAlreadyActive_ShouldThrowConflictException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        var subscription = TestDataHelper.CreateTestSubscription(subscriptionId);
        subscription.Status = SubscriptionStatus.Active;

        SetupMockCollection(new List<Subscription> { subscription });
        SetupSuccessfulTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<ConflictCustomException>(() =>
            _subscriptionService.ActivateSubscriptionAsync(subscriptionId));
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_WhenNoSubscriptionPlan_ShouldThrowBadRequestException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        var subscription = TestDataHelper.CreateTestSubscription(subscriptionId);
        subscription.Status = SubscriptionStatus.Inactive;

        SetupMockCollection(new List<Subscription> { subscription });
        SetupMockCollection<SubscriptionPlan>(); // Empty collection - no subscription plan
        SetupSuccessfulTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestCustomException>(() =>
            _subscriptionService.ActivateSubscriptionAsync(subscriptionId));
    }

    [Fact]
    public async Task UpdateMetadataSubscriptionAsync_WithValidRequest_ShouldUpdateSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString();
        var request = new UpdateMetdataSubscriptionRequest
        {
            SubscriptionId = subscriptionId,
            Name = "Updated Name",
            Description = "Updated Description",
            Code = "UPDATED_CODE",
            Amount = 29.99m,
            Currency = CurrencyType.usd
        };

        var mockSubscriptionCollection = SetupMockCollection<Subscription>();
        mockSubscriptionCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Subscription>>(),
                It.IsAny<UpdateDefinition<Subscription>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        await _subscriptionService.UpdateMetadataSubscriptionAsync(request);

        // Assert
        mockSubscriptionCollection.Verify(x => x.UpdateOneAsync(
            It.IsAny<FilterDefinition<Subscription>>(),
            It.IsAny<UpdateDefinition<Subscription>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMetadataSubscriptionAsync_WhenSubscriptionNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new UpdateMetdataSubscriptionRequest
        {
            SubscriptionId = Guid.NewGuid().ToString(),
            Name = "Updated Name"
        };

        var mockSubscriptionCollection = SetupMockCollection<Subscription>();
        mockSubscriptionCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Subscription>>(),
                It.IsAny<UpdateDefinition<Subscription>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null)); // MatchedCount = 0

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundCustomException>(() =>
            _subscriptionService.UpdateMetadataSubscriptionAsync(request));
    }

    [Fact]
    public async Task UpdateMetadataSubscriptionAsync_WhenNoChanges_ShouldThrowUnprocessableEntityException()
    {
        // Arrange
        var request = new UpdateMetdataSubscriptionRequest
        {
            SubscriptionId = Guid.NewGuid().ToString(),
            Name = "Updated Name"
        };

        var mockSubscriptionCollection = SetupMockCollection<Subscription>();
        mockSubscriptionCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Subscription>>(),
                It.IsAny<UpdateDefinition<Subscription>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 0, null)); // ModifiedCount = 0

        // Act & Assert
        await Assert.ThrowsAsync<UnprocessableEntityCustomException>(() =>
            _subscriptionService.UpdateMetadataSubscriptionAsync(request));
    }

    [Theory]
    [InlineData(SubscriptionTier.Free)]
    [InlineData(SubscriptionTier.Premium)]
    [InlineData(SubscriptionTier.Pro)]
    public async Task CreateSubscriptionAsync_WithDifferentTiers_ShouldSetCorrectVersion(SubscriptionTier tier)
    {
        // Arrange
        var request = new CreateSubscriptionRequest
        {
            Name = $"{tier} Subscription",
            Description = $"{tier} features",
            Code = tier.ToString().ToUpper(),
            Price = tier == SubscriptionTier.Free ? 0 : 19.99m,
            Tier = tier
        };

        var mockSubscriptionCollection = SetupMockCollection<Subscription>();
        
        // Create a simple mock for the full document find result first
        var mockFindFluent = new Mock<IFindFluent<Subscription, Subscription>>();
        var mockProjectedFindFluent = new Mock<IFindFluent<Subscription, int>>();
        
        mockProjectedFindFluent.Setup(x => x.FirstOrDefaultAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(0); // No existing subscriptions
        
        mockFindFluent.Setup(x => x.Project<int>(It.IsAny<ProjectionDefinition<Subscription, int>>()))
                     .Returns(mockProjectedFindFluent.Object);
        
        // Mock no existing subscriptions for this tier
        mockSubscriptionCollection.Setup(x => x.Find(It.IsAny<FilterDefinition<Subscription>>(), It.IsAny<FindOptions>()))
                                 .Returns(mockFindFluent.Object);

        // Act
        await _subscriptionService.CreateSubscriptionAsync(request);

        // Assert
        mockSubscriptionCollection.Verify(x => x.InsertOneAsync(
            It.Is<Subscription>(s => 
                s.Tier == tier && 
                s.Version == 1), // Should be 1 for new tier
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}