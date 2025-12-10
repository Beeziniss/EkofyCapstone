using EkofyApp.Tests.Helpers;
using EkofyApp.Tests.Services;

namespace EkofyApp.Tests;

/// <summary>
/// Test collection to group all service tests
/// This allows xUnit to run service tests in parallel as each test class
/// will run in separate processes but tests within a class run sequentially
/// </summary>
[CollectionDefinition("ServiceTests")]
public class ServiceTestCollection : ICollectionFixture<ServiceTestCollection>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[Collection("ServiceTests")]
public class ServiceIntegrationTests : BaseServiceTest
{
    [Fact]
    public void SetupShouldCreateMockObjects()
    {
        // Verify base setup works
        MockUnitOfWork.Should().NotBeNull();
        MockHttpContextAccessor.Should().NotBeNull();
        MockRedisCacheService.Should().NotBeNull();
        MockHttpContext.Should().NotBeNull();
    }

    [Fact]
    public void SetupMockCollection_ShouldCreateValidMockCollection()
    {
        // Arrange
        var testData = new List<User>
        {
            TestDataHelper.CreateTestUser(),
            TestDataHelper.CreateTestUser()
        };

        // Act
        var mockCollection = SetupMockCollection(testData);

        // Assert
        mockCollection.Should().NotBeNull();
        mockCollection.Object.Should().NotBeNull();
    }

    [Fact]
    public void TestDataHelper_ShouldCreateValidTestData()
    {
        // Arrange & Act
        var user = TestDataHelper.CreateTestUser();
        var artist = TestDataHelper.CreateTestArtist();
        var listener = TestDataHelper.CreateTestListener();
        var track = TestDataHelper.CreateTestTrack();
        var playlist = TestDataHelper.CreateTestPlaylist();
        var category = TestDataHelper.CreateTestCategory();
        var subscription = TestDataHelper.CreateTestSubscription();

        // Assert
        user.Should().NotBeNull();
        artist.Should().NotBeNull();
        listener.Should().NotBeNull();
        track.Should().NotBeNull();
        playlist.Should().NotBeNull();
        category.Should().NotBeNull();
        subscription.Should().NotBeNull();
    }
}