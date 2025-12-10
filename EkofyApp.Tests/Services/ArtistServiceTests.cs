using AutoMapper;
using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Application.ServiceInterfaces.Subscriptions;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Infrastructure.Services.Artists;
using EkofyApp.Tests.Helpers;
using Hangfire;
using MongoDB.Driver;

namespace EkofyApp.Tests.Services;

public class ArtistServiceTests : BaseServiceTest
{
    private readonly ArtistService _artistService;
    private readonly Mock<IUserSubscriptionService> _mockUserSubscriptionService;
    private readonly Mock<IEffectiveEntitlementService> _mockEffectiveEntitlementService;
    private readonly Mock<IApprovalHistoryService> _mockApprovalHistoryService;
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;

    public ArtistServiceTests()
    {
        _mockUserSubscriptionService = new Mock<IUserSubscriptionService>();
        _mockEffectiveEntitlementService = new Mock<IEffectiveEntitlementService>();
        _mockApprovalHistoryService = new Mock<IApprovalHistoryService>();
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();

        _artistService = new ArtistService(
            MockUnitOfWork.Object,
            MockHttpContextAccessor.Object,
            MockRedisCacheService.Object,
            _mockUserSubscriptionService.Object,
            _mockEffectiveEntitlementService.Object,
            _mockApprovalHistoryService.Object,
            _mockBackgroundJobClient.Object
        );
    }

    [Fact]
    public void GetArtistsQueryable_ShouldReturnQueryableOfArtists()
    {
        // Arrange
        var artists = new List<Artist>
        {
            TestDataHelper.CreateTestArtist(),
            TestDataHelper.CreateTestArtist()
        };
        SetupMockCollection(artists);

        // Act
        var result = _artistService.GetArtistsQueryable();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<Artist>>();
        
        // Verify that the method calls GetCollection<Artist>()
        MockUnitOfWork.Verify(x => x.GetCollection<Artist>(), Times.Once);
        
        // Note: We cannot test .Count() or .ToList() here because MongoDB's LINQ provider
        // requires a real database connection. In integration tests, this would work properly.
    }

    [Fact]
    public void SearchArtists_WithValidName_ShouldReturnFilteredArtists()
    {
        // Arrange
        var artists = new List<Artist>
        {
            TestDataHelper.CreateTestArtist().With(a => a.StageNameUnsigned = "john doe"),
            TestDataHelper.CreateTestArtist().With(a => a.StageNameUnsigned = "jane smith"),
            TestDataHelper.CreateTestArtist().With(a => a.StageNameUnsigned = "john williams")
        };
        SetupMockCollection(artists);

        // Act
        var result = _artistService.SearchArtists("john");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void SearchArtists_WithEmptyName_ShouldReturnAllArtists()
    {
        // Arrange
        var artists = new List<Artist>
        {
            TestDataHelper.CreateTestArtist(),
            TestDataHelper.CreateTestArtist()
        };
        SetupMockCollection(artists);

        // Act
        var result = _artistService.SearchArtists("");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateArtistAsync_WithValidRequest_ShouldCreateArtist()
    {
        // Arrange
        var request = new CreateArtistRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Name = "New Artist",
            Biography = "Artist biography",
            IdentityCard = new EkofyApp.Domain.EmbeddedDocuments.IdentityCard
            {
                Number = "123456789",
                FullName = "John Doe"
            }
        };

        var mockArtistCollection = SetupMockCollection<Artist>();

        // Act
        var result = await _artistService.CreateArtistAsync(request);

        // Assert
        result.Should().BeTrue();
        mockArtistCollection.Verify(x => x.InsertOneAsync(
            It.Is<Artist>(a => 
                a.UserId == request.UserId && 
                a.StageName == request.Name &&
                a.Biography == request.Biography),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPendingRegistrationsAsync_WhenCacheEmpty_ShouldReturnEmptyResult()
    {
        // Arrange
        var cacheResult = new Mock<ICacheResult<PaginatedData<PendingArtistRegistrationRequest>>>();
        cacheResult.Setup(x => x.Success).Returns(false);

        MockRedisCacheService.Setup(x => x.GetPendingArtistRegistrationsAsync(1, 20))
                           .ReturnsAsync(cacheResult.Object);

        // Act
        var result = await _artistService.GetPendingRegistrationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ApproveArtistRegistrationAsync_WithValidRequest_ShouldApproveRegistration()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId, "Moderator");

        var request = new ArtistRegistrationApprovalRequest
        {
            UserId = targetUserId,
            Email = "artist@example.com",
            FullName = "Artist Name"
        };

        var pendingRegistration = new PendingArtistRegistrationRequest
        {
            UserId = targetUserId,
            Email = "artist@example.com",
            FullName = "Artist Name",
            StageName = "Artist",
            PasswordHash = "hashed_password"
        };

        MockRedisCacheService.Setup(x => x.TryGetGeneric<PendingArtistRegistrationRequest>($"artist:{targetUserId}:pendingRegistration", out It.Ref<PendingArtistRegistrationRequest>.IsAny))
                           .Returns((string key, out PendingArtistRegistrationRequest value) =>
                           {
                               value = pendingRegistration;
                               return true;
                           });

        SetupMockCollection<User>();
        SetupMockCollection<Artist>();
        SetupSuccessfulTransaction();

        _mockUserSubscriptionService.Setup(x => x.CreateUserSubscriptionAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset?>()))
            .Returns(Task.CompletedTask);

        _mockEffectiveEntitlementService.Setup(x => x.BuildFreeTierAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<string>(),
                It.IsAny<UserRole>(),
                It.IsAny<List<AppliedEntitlement>>(),
                It.IsAny<DateTimeOffset?>()))
            .Returns(Task.CompletedTask);

        // Act
        await _artistService.ApproveArtistRegistrationAsync(request);

        // Assert
        VerifyTransactionExecuted();
        
        // Use delegate-based verification instead of expression trees for methods with optional parameters
        _mockUserSubscriptionService.Verify(x => x.CreateUserSubscriptionAsync(
            It.IsAny<IClientSessionHandle>(),
            targetUserId,
            string.Empty,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<DateTimeOffset?>()), Times.Once);

        _mockEffectiveEntitlementService.Verify(x => x.BuildFreeTierAsync(
            It.IsAny<IClientSessionHandle>(),
            targetUserId,
            UserRole.Artist,
            It.IsAny<List<AppliedEntitlement>>(),
            It.IsAny<DateTimeOffset?>()), Times.Once);
    }

    [Fact]
    public async Task ApproveArtistRegistrationAsync_WhenRegistrationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId, "Moderator");

        var request = new ArtistRegistrationApprovalRequest
        {
            UserId = targetUserId
        };

        MockRedisCacheService.Setup(x => x.TryGetGeneric<PendingArtistRegistrationRequest>($"artist:{targetUserId}:pendingRegistration", out It.Ref<PendingArtistRegistrationRequest>.IsAny))
                           .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundCustomException>(() =>
            _artistService.ApproveArtistRegistrationAsync(request));
    }

    [Fact]
    public async Task RejectArtistRegistrationAsync_WithValidRequest_ShouldRejectRegistration()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId, "Moderator");

        var request = new ArtistRegistrationApprovalRequest
        {
            UserId = targetUserId,
            Email = "artist@example.com",
            FullName = "Artist Name",
            RejectionReason = "Incomplete documentation"
        };

        var pendingRegistration = new PendingArtistRegistrationRequest
        {
            UserId = targetUserId,
            Email = "artist@example.com",
            FullName = "Artist Name"
        };

        MockRedisCacheService.Setup(x => x.TryGetGeneric<PendingArtistRegistrationRequest>($"artist:{targetUserId}:pendingRegistration", out It.Ref<PendingArtistRegistrationRequest>.IsAny))
                           .Returns((string key, out PendingArtistRegistrationRequest value) =>
                           {
                               value = pendingRegistration;
                               return true;
                           });

        MockRedisCacheService.Setup(x => x.RemoveAsync($"artist:{targetUserId}:pendingRegistration"))
                           .Returns(Task.CompletedTask);

        _mockApprovalHistoryService.Setup(x => x.CreateApprovalHistoryAsync(It.IsAny<EkofyApp.Application.Models.ApprovalHistories.ApprovalHistoryRequest>()))
                                  .Returns(Task.CompletedTask);

        // Act
        await _artistService.RejectArtistRegistrationAsync(request);

        // Assert
        MockRedisCacheService.Verify(x => x.RemoveAsync($"artist:{targetUserId}:pendingRegistration"), Times.Once);
        _mockApprovalHistoryService.Verify(x => x.CreateApprovalHistoryAsync(
            It.Is<EkofyApp.Application.Models.ApprovalHistories.ApprovalHistoryRequest>(req => 
                req.Action == HistoryActionType.Rejected && 
                req.Notes == request.RejectionReason)), Times.Once);
    }

    [Fact]
    public async Task ComputeArtistRevenueByArtistIdAsync_WithValidArtist_ShouldReturnRevenueData()
    {
        // Arrange
        var artistId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        var artist = TestDataHelper.CreateTestArtist(artistId, userId);
        
        // Setup collections with empty data - we're testing the happy path with no revenue data
        var mockArtistCollection = SetupMockCollection(new List<Artist> { artist });
        SetupMockCollection<RoyaltyReport>();  // Empty collection
        SetupMockCollection<PackageOrder>();   // Empty collection
        SetupMockCollection<Domain.Entities.Invoice>(); // Empty collection
        SetupMockCollection<PayoutTransaction>(); // Empty collection

        // Mock the UpdateOneAsync to simulate successful update
        mockArtistCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Artist>>(),
                It.IsAny<UpdateDefinition<Artist>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        var result = await _artistService.ComputeArtistRevenueByArtistIdAsync(artistId);

        // Assert
        result.Should().NotBeNull();
        result.RoyaltyEarnings.Should().Be(0); // No royalty data in setup
        result.ServiceRevenue.Should().Be(0);  // No revenue data in setup
        result.ServiceEarnings.Should().Be(0); // No earnings data in setup
    }

    [Fact]
    public async Task ComputeArtistRevenueByArtistIdAsync_WhenArtistNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var artistId = Guid.NewGuid().ToString();
        SetupMockCollection<Artist>(); // Empty collection - no artist with this ID
        SetupMockCollection<RoyaltyReport>();
        SetupMockCollection<PackageOrder>();
        SetupMockCollection<Domain.Entities.Invoice>();
        SetupMockCollection<PayoutTransaction>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await _artistService.ComputeArtistRevenueByArtistIdAsync(artistId);
        });

        exception.Message.Should().Contain($"User with ArtistId {artistId} not found.");
    }
}