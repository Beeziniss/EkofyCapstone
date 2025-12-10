using EkofyApp.Application.Models.Categories;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Infrastructure.Services.Categories;
using EkofyApp.Tests.Helpers;
using MongoDB.Driver;

namespace EkofyApp.Tests.Services;

public class CategoryServiceTests : BaseServiceTest
{
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _categoryService = new CategoryService(MockUnitOfWork.Object);
    }

    [Fact]
    public void GetCategories_ShouldReturnQueryableOfCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            TestDataHelper.CreateTestCategory(type: CategoryType.Genre),
            TestDataHelper.CreateTestCategory(type: CategoryType.Mood)
        };
        SetupMockCollection(categories);

        // Act
        var result = _categoryService.GetCategories();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateCategoryAsync_WithValidRequest_ShouldCreateCategory()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Electronic",
            Description = "Electronic music genre",
            Type = CategoryType.Genre
        };

        var mockCategoryCollection = SetupMockCollection<Category>();

        // Act
        await _categoryService.CreateCategoryAsync(request);

        // Assert
        mockCategoryCollection.Verify(x => x.InsertOneAsync(
            It.Is<Category>(c => 
                c.Name == request.Name && 
                c.Description == request.Description &&
                c.Type == request.Type &&
                c.Slug == "electronic" &&
                c.Popularity == 0),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithValidRequest_ShouldUpdateCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid().ToString();
        var existingCategory = TestDataHelper.CreateTestCategory(categoryId);
        
        var request = new UpdateCategoryRequest
        {
            CategoryId = categoryId,
            Name = "Updated Electronic",
            Description = "Updated description",
            Type = CategoryType.Mood,
            Popularity = 100,
            IsVisible = false
        };

        SetupMockCollection(new List<Category> { existingCategory });
        var mockCategoryCollection = SetupMockCollection(new List<Category> { existingCategory });
        
        mockCategoryCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Category>>(),
                It.IsAny<UpdateDefinition<Category>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        await _categoryService.UpdateCategoryAsync(request);

        // Assert
        mockCategoryCollection.Verify(x => x.UpdateOneAsync(
            It.IsAny<FilterDefinition<Category>>(),
            It.IsAny<UpdateDefinition<Category>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            CategoryId = Guid.NewGuid().ToString(),
            Name = "Updated Name"
        };

        SetupMockCollection<Category>(); // Empty collection

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundCustomException>(() =>
            _categoryService.UpdateCategoryAsync(request));
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithNoValidFields_ShouldThrowBadRequestException()
    {
        // Arrange
        var categoryId = Guid.NewGuid().ToString();
        var existingCategory = TestDataHelper.CreateTestCategory(categoryId);
        
        var request = new UpdateCategoryRequest
        {
            CategoryId = categoryId
            // No fields to update
        };

        SetupMockCollection(new List<Category> { existingCategory });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestCustomException>(() =>
            _categoryService.UpdateCategoryAsync(request));
    }

    [Fact]
    public async Task SoftDeleteCategoryAsync_WithValidId_ShouldSoftDeleteCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid().ToString();
        var existingCategory = TestDataHelper.CreateTestCategory(categoryId);
        
        SetupMockCollection(new List<Category> { existingCategory });
        var mockCategoryCollection = SetupMockCollection(new List<Category> { existingCategory });
        
        mockCategoryCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<Category>>(),
                It.IsAny<UpdateDefinition<Category>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        await _categoryService.SoftDeleteCategoryAsync(categoryId);

        // Assert
        mockCategoryCollection.Verify(x => x.UpdateOneAsync(
            It.IsAny<FilterDefinition<Category>>(),
            It.IsAny<UpdateDefinition<Category>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteCategoryAsync_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var categoryId = Guid.NewGuid().ToString();
        SetupMockCollection<Category>(); // Empty collection

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundCustomException>(() =>
            _categoryService.SoftDeleteCategoryAsync(categoryId));
    }

    [Fact]
    public async Task HardDeleteCategoryAsync_WithValidRequest_ShouldDeleteCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid().ToString();
        var existingCategory = TestDataHelper.CreateTestCategory(categoryId);
        
        var request = new DeleteCategoryRequest { CategoryId = categoryId };

        SetupMockCollection(new List<Category> { existingCategory });
        var mockCategoryCollection = SetupMockCollection(new List<Category> { existingCategory });
        
        mockCategoryCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<Category>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        // Act
        await _categoryService.HardDeleteCategoryAsync(request);

        // Assert
        mockCategoryCollection.Verify(x => x.DeleteOneAsync(
            It.IsAny<FilterDefinition<Category>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HardDeleteCategoryAsync_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = new DeleteCategoryRequest { CategoryId = Guid.NewGuid().ToString() };
        SetupMockCollection<Category>(); // Empty collection

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundCustomException>(() =>
            _categoryService.HardDeleteCategoryAsync(request));
    }

    [Fact]
    public async Task HardDeleteCategoryAsync_WhenDeletionFails_ShouldThrowBadRequestException()
    {
        // Arrange
        var categoryId = Guid.NewGuid().ToString();
        var existingCategory = TestDataHelper.CreateTestCategory(categoryId);
        
        var request = new DeleteCategoryRequest { CategoryId = categoryId };

        SetupMockCollection(new List<Category> { existingCategory });
        var mockCategoryCollection = SetupMockCollection(new List<Category> { existingCategory });
        
        mockCategoryCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<Category>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(0)); // DeletedCount = 0

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestCustomException>(() =>
            _categoryService.HardDeleteCategoryAsync(request));
    }

    [Fact]
    public async Task GetMoodsFromAudioFeaturesAsync_WithValidMoods_ShouldReturnMoodCategoryIds()
    {
        // Arrange
        var moodTypes = new List<MoodType> { MoodType.Happy, MoodType.Energetic };
        var moodCategories = new List<Category>
        {
            new() { Id = "mood1", Name = "Happy", Type = CategoryType.Mood },
            new() { Id = "mood2", Name = "Energetic", Type = CategoryType.Mood }
        };

        SetupMockCollection(moodCategories);

        // Act
        var result = await _categoryService.GetMoodsFromAudioFeaturesAsync(moodTypes);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain("mood1");
        result.Should().Contain("mood2");
    }

    [Fact]
    public async Task GetMoodsFromAudioFeaturesAsync_WithEmptyMoods_ShouldReturnEmpty()
    {
        // Arrange
        var moodTypes = new List<MoodType>();

        // Act
        var result = await _categoryService.GetMoodsFromAudioFeaturesAsync(moodTypes);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(120, 1, 0.6, 0.7, true)] // Happy: Fast tempo, major mode, high energy and danceability
    [InlineData(80, 1, 0.6, 0.2, false)] // Calm: Slow tempo, low acousticness
    [InlineData(70, 0, 0.2, 0.1, false)] // Sad: Very slow, minor mode, low energy
    public void DetectMoods_WithDifferentAudioFeatures_ShouldDetectCorrectMoods(
        float tempo, int modeNumber, double acousticness, double energy, bool shouldDetectHappy)
    {
        // Arrange
        var audioFeature = new AudioFeature
        {
            Tempo = tempo,
            ModeNumber = modeNumber,
            Acousticness = (float)acousticness,
            Energy = (float)energy,
            Danceability = 0.6f,
            SpectralCentroid = 2500,
            ZeroCrossingRate = 0.03f,
            MfccMean = new List<float> { 10, 20, 30 },
            ChromaMean = new List<float> { 0.6f, 0.7f, 0.8f }
        };

        // Act
        var result = _categoryService.DetectMoods(audioFeature);

        // Assert
        result.Should().NotBeNull();
        if (shouldDetectHappy)
        {
            result.Should().Contain(MoodType.Happy);
        }
        else
        {
            result.Should().NotContain(MoodType.Happy);
        }
    }

    [Fact]
    public void DetectMoods_WithCalmFeatures_ShouldDetectCalm()
    {
        // Arrange
        var audioFeature = new AudioFeature
        {
            Tempo = 80, // Slow tempo
            Acousticness = 0.6f, // High acousticness
            Energy = 0.2f, // Low energy
            ZeroCrossingRate = 0.03f, // Low ZCR
            Danceability = 0.4f,
            ModeNumber = 0,
            SpectralCentroid = 2000,
            MfccMean = new List<float> { 5, 10, 15 },
            ChromaMean = new List<float> { 0.3f, 0.4f, 0.5f }
        };

        // Act
        var result = _categoryService.DetectMoods(audioFeature);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(MoodType.Calm);
    }

    [Fact]
    public void DetectMoods_WithEnergeticFeatures_ShouldDetectEnergetic()
    {
        // Arrange
        var audioFeature = new AudioFeature
        {
            Tempo = 130, // Fast tempo
            Energy = 0.8f, // High energy
            Danceability = 0.7f, // High danceability
            Acousticness = 0.2f,
            ModeNumber = 1,
            SpectralCentroid = 3000,
            ZeroCrossingRate = 0.05f,
            MfccMean = new List<float> { 20, 30, 40 },
            ChromaMean = new List<float> { 0.7f, 0.8f, 0.9f }
        };

        // Act
        var result = _categoryService.DetectMoods(audioFeature);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(MoodType.Energetic);
    }

    [Fact]
    public void GenerateAlternativeDescription_WithValidFeatures_ShouldGenerateDescription()
    {
        // Arrange
        var audioFeature = new AudioFeature
        {
            Tempo = 120,
            Energy = 0.7f,
            Danceability = 0.6f,
            Acousticness = 0.3f,
            ModeNumber = 1, // Major mode
            SpectralCentroid = 3000,
            MfccMean = new List<float> { 20, 30, 40 }
        };

        var moods = new List<MoodType> { MoodType.Happy, MoodType.Energetic };

        // Act
        var result = _categoryService.GenerateAlternativeDescription(audioFeature, moods);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Bài hát này có nh?p ??");
        result.Should().Contain("tông tr??ng"); // Major mode description
        result.Should().Contain("Vui t??i"); // Happy mood translation
        result.Should().Contain("Tràn ??y n?ng l??ng"); // Energetic mood translation
    }

    [Fact]
    public void GenerateAlternativeDescription_WithSlowTempo_ShouldDescribeAsSlowAndGentle()
    {
        // Arrange
        var audioFeature = new AudioFeature
        {
            Tempo = 70, // Very slow
            Energy = 0.2f,
            Danceability = 0.3f,
            Acousticness = 0.8f,
            ModeNumber = 0,
            SpectralCentroid = 1800,
            MfccMean = new List<float> { -10, -5, 0 }
        };

        var moods = new List<MoodType> { MoodType.Sad };

        // Act
        var result = _categoryService.GenerateAlternativeDescription(audioFeature, moods);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("r?t ch?m và nh? nhàng");
        result.Should().Contain("Bu?n bã");
    }

    [Fact]
    public void GenerateAlternativeDescription_WithNoMoods_ShouldUseDefaultMoodDescription()
    {
        // Arrange
        var audioFeature = new AudioFeature
        {
            Tempo = 100,
            Energy = 0.5f,
            Danceability = 0.5f,
            Acousticness = 0.5f,
            ModeNumber = 1,
            SpectralCentroid = 2500,
            MfccMean = new List<float> { 10, 15, 20 }
        };

        var moods = new List<MoodType>(); // Empty moods

        // Act
        var result = _categoryService.GenerateAlternativeDescription(audioFeature, moods);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Chung"); // Default mood description
    }
}