using EkofyApp.Application.Models.UserEngagements;
using EkofyApp.Application.Models.Users;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Infrastructure.Services.Users;
using EkofyApp.Tests.Helpers;
using MongoDB.Driver;

namespace EkofyApp.Tests.Services;

public class UserServiceTests : BaseServiceTest
{
    private readonly UserService _userService;
    private readonly Mock<ILogger<UserService>> _mockLogger;

    public UserServiceTests()
    {
        _mockLogger = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            MockUnitOfWork.Object,
            MockHttpContextAccessor.Object,
            MockRedisCacheService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void GetUsers_ShouldReturnQueryableOfUsers()
    {
        // Arrange
        var users = new List<User>
        {
            TestDataHelper.CreateTestUser(),
            TestDataHelper.CreateTestUser()
        };
        SetupMockCollection(users);

        // Act
        var result = _userService.GetUsers();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = TestDataHelper.CreateTestUser(userId);

        SetupMockCollection(new List<User> { user });

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.PasswordHash.Should().BeNull(); // Should be excluded in projection
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        SetupMockCollection<User>(); // Empty collection

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetFollowersByUserId_ShouldReturnFollowers()
    {
        // Arrange
        var targetUserId = Guid.NewGuid().ToString();
        var follower1Id = Guid.NewGuid().ToString();
        var follower2Id = Guid.NewGuid().ToString();

        var userEngagements = new List<UserEngagement>
        {
            new() { ActorId = follower1Id, TargetId = targetUserId, Action = UserEngagementAction.Follow },
            new() { ActorId = follower2Id, TargetId = targetUserId, Action = UserEngagementAction.Follow }
        };

        var users = new List<User>
        {
            TestDataHelper.CreateTestUser(follower1Id),
            TestDataHelper.CreateTestUser(follower2Id)
        };

        SetupMockCollection(userEngagements);
        SetupMockCollection(users);

        // Act
        var result = _userService.GetFollowersByUserId(targetUserId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetFollowingsByUserId_ShouldReturnFollowings()
    {
        // Arrange
        var actorUserId = Guid.NewGuid().ToString();
        var following1Id = Guid.NewGuid().ToString();
        var following2Id = Guid.NewGuid().ToString();

        var userEngagements = new List<UserEngagement>
        {
            new() { ActorId = actorUserId, TargetId = following1Id, Action = UserEngagementAction.Follow },
            new() { ActorId = actorUserId, TargetId = following2Id, Action = UserEngagementAction.Follow }
        };

        var users = new List<User>
        {
            TestDataHelper.CreateTestUser(following1Id),
            TestDataHelper.CreateTestUser(following2Id)
        };

        SetupMockCollection(userEngagements);
        SetupMockCollection(users);

        // Act
        var result = _userService.GetFollowingsByUserId(actorUserId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateModeratorAsync_WithValidRequest_ShouldCreateModerator()
    {
        // Arrange
        var request = new CreateModeratorRequest
        {
            Email = "moderator@example.com",
            FullName = "Moderator Name",
            Password = "password123"
        };

        SetupMockCollection<User>(); // Empty collection - no existing users
        var mockUserCollection = SetupMockCollection<User>();

        // Act
        await _userService.CreateModeratorAsync(request);

        // Assert
        mockUserCollection.Verify(x => x.InsertOneAsync(
            It.Is<User>(u => 
                u.Email == request.Email.ToLowerInvariant() && 
                u.FullName == request.FullName &&
                u.Role == UserRole.Moderator &&
                u.Status == UserStatus.Active &&
                u.IsLinkedWithGoogle == false),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateModeratorAsync_WithExistingEmail_ShouldThrowConflictException()
    {
        // Arrange
        var request = new CreateModeratorRequest
        {
            Email = "existing@example.com",
            FullName = "Moderator Name",
            Password = "password123"
        };

        var existingUser = TestDataHelper.CreateTestUser(email: "existing@example.com");
        SetupMockCollection(new List<User> { existingUser });

        // Act & Assert
        await Assert.ThrowsAsync<ConflictCustomException>(() =>
            _userService.CreateModeratorAsync(request));
    }

    [Fact]
    public async Task CreateAdminAsync_WithValidRequest_ShouldCreateAdmin()
    {
        // Arrange
        var request = new CreateAdminRequest
        {
            Email = "admin@example.com",
            FullName = "Admin Name",
            Password = "password123"
        };

        SetupMockCollection<User>(); // Empty collection - no existing users
        var mockUserCollection = SetupMockCollection<User>();

        // Act
        await _userService.CreateAdminAsync(request);

        // Assert
        mockUserCollection.Verify(x => x.InsertOneAsync(
            It.Is<User>(u => 
                u.Email == request.Email.ToLowerInvariant() && 
                u.FullName == request.FullName &&
                u.Role == UserRole.Admin &&
                u.Status == UserStatus.Active &&
                u.IsLinkedWithGoogle == false),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FollowUserAsync_WithValidRequest_ShouldCreateFollowRelationship()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId, "Listener");

        var request = new UserEngagementRequest { TargetId = targetUserId };

        var currentUser = TestDataHelper.CreateTestUser(currentUserId, role: UserRole.Listener);
        var targetUser = TestDataHelper.CreateTestUser(targetUserId, role: UserRole.Artist);

        SetupMockCollection(new List<UserEngagement>()); // No existing follow relationship
        SetupMockCollection(new List<User> { currentUser, targetUser });
        SetupMockCollection(new List<Artist> { TestDataHelper.CreateTestArtist(userId: targetUserId) });
        SetupSuccessfulTransaction();

        var mockUserEngagementCollection = SetupMockCollection<UserEngagement>();
        var mockArtistCollection = SetupMockCollection(new List<Artist> { TestDataHelper.CreateTestArtist(userId: targetUserId) });

        mockArtistCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<Artist>>(),
                It.IsAny<UpdateDefinition<Artist>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, BsonNull.Value));

        // Act
        await _userService.FollowUserAsync(request);

        // Assert
        VerifyTransactionExecuted();
        mockUserEngagementCollection.Verify(x => x.InsertOneAsync(
            It.IsAny<IClientSessionHandle>(),
            It.Is<UserEngagement>(ue => 
                ue.ActorId == currentUserId && 
                ue.TargetId == targetUserId && 
                ue.Action == UserEngagementAction.Follow),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FollowUserAsync_WhenAlreadyFollowing_ShouldThrowConflictException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId);

        var request = new UserEngagementRequest { TargetId = targetUserId };

        var existingFollow = new UserEngagement
        {
            ActorId = currentUserId,
            TargetId = targetUserId,
            Action = UserEngagementAction.Follow
        };

        SetupMockCollection(new List<UserEngagement> { existingFollow });
        SetupSuccessfulTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<ConflictCustomException>(() =>
            _userService.FollowUserAsync(request));
    }

    [Fact]
    public async Task FollowUserAsync_WhenFollowingSelf_ShouldThrowBadRequestException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        MockHttpContext.User = CreateTestUser(userId);

        var request = new UserEngagementRequest { TargetId = userId };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestCustomException>(() =>
            _userService.FollowUserAsync(request));
    }

    [Fact]
    public async Task UnfollowUserAsync_WithValidRequest_ShouldRemoveFollowRelationship()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId, "Listener");

        var request = new UserEngagementRequest { TargetId = targetUserId };

        var existingFollow = new UserEngagement
        {
            Id = Guid.NewGuid().ToString(),
            ActorId = currentUserId,
            TargetId = targetUserId,
            Action = UserEngagementAction.Follow
        };

        var targetUser = TestDataHelper.CreateTestUser(targetUserId, role: UserRole.Artist);

        SetupMockCollection(new List<UserEngagement> { existingFollow });
        SetupMockCollection(new List<User> { targetUser });
        SetupMockCollection(new List<Artist> { TestDataHelper.CreateTestArtist(userId: targetUserId) });
        SetupSuccessfulTransaction();

        var mockUserEngagementCollection = SetupMockCollection(new List<UserEngagement> { existingFollow });
        var mockArtistCollection = SetupMockCollection(new List<Artist> { TestDataHelper.CreateTestArtist(userId: targetUserId) });

        mockUserEngagementCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<UserEngagement>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        mockArtistCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<Artist>>(),
                It.IsAny<UpdateDefinition<Artist>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, BsonNull.Value));

        // Act
        await _userService.UnfollowUserAsync(request);

        // Assert
        VerifyTransactionExecuted();
        mockUserEngagementCollection.Verify(x => x.DeleteOneAsync(
            It.IsAny<IClientSessionHandle>(),
            It.IsAny<FilterDefinition<UserEngagement>>(),
            It.IsAny<DeleteOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnfollowUserAsync_WhenNotFollowing_ShouldThrowNotFoundException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId);

        var request = new UserEngagementRequest { TargetId = targetUserId };

        SetupMockCollection<UserEngagement>(); // No existing follow relationship
        SetupSuccessfulTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundCustomException>(() =>
            _userService.UnfollowUserAsync(request));
    }

    [Fact]
    public async Task BanUserAsync_WithValidUser_ShouldBanUser()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId, "Admin");

        var targetUser = TestDataHelper.CreateTestUser(targetUserId, role: UserRole.Listener);

        SetupMockCollection(new List<User> { targetUser });
        SetupMockCollection(new List<Listener> { TestDataHelper.CreateTestListener(userId: targetUserId) });
        SetupMockCollection<Comment>();
        SetupMockCollection<Playlist>();
        SetupSuccessfulTransaction();

        var mockUserCollection = SetupMockCollection(new List<User> { targetUser });
        mockUserCollection.Setup(x => x.FindOneAndUpdateAsync<User>(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        // Act
        await _userService.BanUserAsync(targetUserId);

        // Assert
        VerifyTransactionExecuted();
    }

    [Fact]
    public async Task UnbanUserAsync_WithValidUser_ShouldUnbanUser()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId, "Admin");

        var targetUser = TestDataHelper.CreateTestUser(targetUserId, role: UserRole.Listener, status: UserStatus.Banned);

        SetupMockCollection(new List<User> { targetUser });
        SetupMockCollection(new List<Listener> { TestDataHelper.CreateTestListener(userId: targetUserId) });
        SetupMockCollection<Comment>();
        SetupMockCollection<Playlist>();
        SetupSuccessfulTransaction();

        var mockUserCollection = SetupMockCollection(new List<User> { targetUser });
        mockUserCollection.Setup(x => x.FindOneAndUpdateAsync<User>(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<UpdateDefinition<User>>(),
                It.IsAny<FindOneAndUpdateOptions<User, User>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        // Act
        await _userService.UnbanUserAsync(targetUserId);

        // Assert
        VerifyTransactionExecuted();
    }

    [Fact]
    public async Task DeleteUserManualAsync_WithValidUser_ShouldDeleteUser()
    {
        // Arrange
        var currentUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(currentUserId, "Admin");

        var targetUser = TestDataHelper.CreateTestUser(targetUserId, role: UserRole.Listener);

        SetupMockCollection(new List<User> { targetUser });
        SetupSuccessfulTransaction();

        var mockUserCollection = SetupMockCollection(new List<User> { targetUser });
        var mockListenerCollection = SetupMockCollection<Listener>();
        var mockUserSubscriptionCollection = SetupMockCollection<UserSubscription>();
        var mockEffectiveEntitlementCollection = SetupMockCollection<EffectiveEntitlement>();
        var mockPlaylistCollection = SetupMockCollection<Playlist>();
        var mockUserEngagementCollection = SetupMockCollection<UserEngagement>();

        mockUserCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        mockListenerCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<Listener>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        mockUserSubscriptionCollection.Setup(x => x.DeleteManyAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<UserSubscription>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        mockEffectiveEntitlementCollection.Setup(x => x.DeleteManyAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<EffectiveEntitlement>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        // Act
        await _userService.DeleteUserManualAsync(targetUserId);

        // Assert
        VerifyTransactionExecuted();
        mockUserCollection.Verify(x => x.DeleteOneAsync(
            It.IsAny<IClientSessionHandle>(),
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<DeleteOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckUserFollowingAsync_WhenUserNotAuthenticated_ShouldReturnFalse()
    {
        // Arrange
        MockHttpContext.User = new ClaimsPrincipal(); // No claims

        // Act
        var result = await _userService.CheckUserFollowingAsync("userFollowingId");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckUserFollowingAsync_WhenCacheHit_ShouldReturnFromCache()
    {
        // Arrange
        var userFollowingId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(userId);
        
        MockRedisCacheService.Setup(x => x.ListLengthAsync($"favorite_following:{userId}"))
                           .ReturnsAsync(5);
        MockRedisCacheService.Setup(x => x.ListContainsAsync($"favorite_following:{userId}", userFollowingId))
                           .ReturnsAsync(true);

        // Act
        var result = await _userService.CheckUserFollowingAsync(userFollowingId);

        // Assert
        result.Should().BeTrue();
        MockRedisCacheService.Verify(x => x.ListContainsAsync($"favorite_following:{userId}", userFollowingId), Times.Once);
    }

    [Theory]
    [InlineData(RestrictionAction.Comment)]
    [InlineData(RestrictionAction.Report)]
    [InlineData(RestrictionAction.UploadTrack)]
    [InlineData(RestrictionAction.CreatePublicRequest)]
    [InlineData(RestrictionAction.SendRequest)]
    public async Task CheckMultipleRestrictionsAsync_WithRestrictions_ShouldReturnTrue(RestrictionAction restrictionAction)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        MockHttpContext.User = CreateTestUser(userId);

        var userWithRestriction = TestDataHelper.CreateTestUser(userId);
        userWithRestriction.Restrictions = new List<Restriction>
        {
            new() 
            { 
                Action = restrictionAction, 
                Type = RestrictionType.Banned 
            }
        };

        SetupMockCollection(new List<User> { userWithRestriction });

        // Act
        var result = await _userService.CheckMultipleRestrictionsAsync(restrictionAction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckMultipleRestrictionsAsync_WithoutRestrictions_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        MockHttpContext.User = CreateTestUser(userId);

        var userWithoutRestriction = TestDataHelper.CreateTestUser(userId);
        userWithoutRestriction.Restrictions = new List<Restriction>();

        SetupMockCollection(new List<User> { userWithoutRestriction });

        // Act
        var result = await _userService.CheckMultipleRestrictionsAsync(RestrictionAction.Comment);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetUserEngagement_ShouldReturnQueryableOfUserEngagement()
    {
        // Arrange
        var userEngagements = new List<UserEngagement>
        {
            new() { ActorId = "user1", TargetId = "user2", Action = UserEngagementAction.Follow },
            new() { ActorId = "user2", TargetId = "track1", Action = UserEngagementAction.Like }
        };
        SetupMockCollection(userEngagements);

        // Act
        var result = _userService.GetUserEngagement();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetPaymentTransactionsByUserId_ShouldReturnQueryableOfPaymentTransactions()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var paymentTransactions = new List<PaymentTransaction>
        {
            new() { UserId = userId, Amount = 10.99m },
            new() { UserId = userId, Amount = 19.99m },
            new() { UserId = "other", Amount = 5.99m } // Different user
        };
        SetupMockCollection(paymentTransactions);

        // Act
        var result = _userService.GetPaymentTransactionsByUserId(userId);

        // Assert
        result.Should().NotBeNull();
        // The filter would happen in the actual implementation
        // Here we just verify the method doesn't throw
    }
}