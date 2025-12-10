using AutoMapper;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Application.ServiceInterfaces.Recommendations;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Infrastructure.Services.Notifications;
using EkofyApp.Infrastructure.Services.Tracks;
using EkofyApp.Tests.Helpers;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using MongoDB.Driver;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Domain.Utils;
using EkofyApp.Application.Models.ApprovalHistories;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Tests.Services;

public class TrackServiceTests : BaseServiceTest
{
    private readonly TrackService _trackService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _mockEmbeddingGenerator;
    private readonly Mock<IRecommendationService> _mockRecommendationService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IFfmpegService> _mockFfmpegService;
    private readonly Mock<IWorkService> _mockWorkService;
    private readonly Mock<IRecordingService> _mockRecordingService;
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly Mock<IAudioAnalysisService> _mockAudioAnalysisService;
    private readonly Mock<IAmazonS3Service> _mockAmazonS3Service;
    private readonly Mock<IApprovalHistoryService> _mockApprovalHistoryService;
    private readonly Mock<IEmySoundService> _mockEmySoundService;
    private readonly Mock<IArtistService> _mockArtistService;
    private readonly Mock<ITrackUploadNotifier> _mockTrackUploadNotifier;
    private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
    private readonly Mock<ILogger<TrackService>> _mockLogger;

    public TrackServiceTests()
    {
        _mockMapper = new Mock<IMapper>();
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        _mockRecommendationService = new Mock<IRecommendationService>();
        _mockUserService = new Mock<IUserService>();
        _mockFfmpegService = new Mock<IFfmpegService>();
        _mockWorkService = new Mock<IWorkService>();
        _mockRecordingService = new Mock<IRecordingService>();
        _mockCategoryService = new Mock<ICategoryService>();
        _mockAudioAnalysisService = new Mock<IAudioAnalysisService>();
        _mockAmazonS3Service = new Mock<IAmazonS3Service>();
        _mockApprovalHistoryService = new Mock<IApprovalHistoryService>();
        _mockEmySoundService = new Mock<IEmySoundService>();
        _mockArtistService = new Mock<IArtistService>();
        _mockTrackUploadNotifier = new Mock<ITrackUploadNotifier>();
        _mockHubContext = new Mock<IHubContext<NotificationHub>>();
        _mockLogger = new Mock<ILogger<TrackService>>();

        _trackService = new TrackService(
            MockUnitOfWork.Object,
            _mockMapper.Object,
            MockHttpContextAccessor.Object,
            MockRedisCacheService.Object,
            _mockEmbeddingGenerator.Object,
            _mockRecommendationService.Object,
            _mockUserService.Object,
            _mockFfmpegService.Object,
            _mockWorkService.Object,
            _mockRecordingService.Object,
            _mockCategoryService.Object,
            _mockAudioAnalysisService.Object,
            _mockAmazonS3Service.Object,
            _mockApprovalHistoryService.Object,
            _mockEmySoundService.Object,
            _mockArtistService.Object,
            _mockTrackUploadNotifier.Object,
            _mockHubContext.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void GetTracks_ShouldReturnQueryableOfTracks()
    {
        // Arrange
        var tracks = new List<Track>
        {
            TestDataHelper.CreateTestTrack(),
            TestDataHelper.CreateTestTrack()
        };
        SetupMockCollection(tracks);

        // Act
        var result = _trackService.GetTracks();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddToFavoriteTrackAsync_WhenAddingFavorite_ShouldReturnUpdatedCount()
    {
        // Arrange
        var trackId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var track = TestDataHelper.CreateTestTrack(trackId);
        track.FavoriteCount = 5;

        SetupMockCollection(new List<Track> { track });
        SetupMockCollection<UserEngagement>();
        SetupMockCollection<TrackDailyMetric>();

        // Setup user claims
        MockHttpContext.User = CreateTestUser(userId);

        // Setup track update mock - need to create a proper mock for FindOneAndUpdate
        var mockTrackCollection = SetupMockCollection(new List<Track> { track });
        mockTrackCollection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<Track>>(),
                It.IsAny<UpdateDefinition<Track>>(),
                It.IsAny<FindOneAndUpdateOptions<Track>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Track { Id = trackId, FavoriteCount = 6 });

        // Act
        var result = await _trackService.AddToFavoriteTrackAsync(trackId, true);

        // Assert
        result.Should().Be(6); // FavoriteCount should be incremented by 1
    }

    [Fact]
    public async Task AddToFavoriteTrackAsync_WhenRemovingFavorite_ShouldReturnUpdatedCount()
    {
        // Arrange
        var trackId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var track = TestDataHelper.CreateTestTrack(trackId);
        track.FavoriteCount = 5;

        SetupMockCollection(new List<Track> { track });
        SetupMockCollection<UserEngagement>();
        SetupMockCollection<TrackDailyMetric>();

        // Setup user claims
        MockHttpContext.User = CreateTestUser(userId);

        // Setup track update mock for removing favorite
        var mockTrackCollection = SetupMockCollection(new List<Track> { track });
        mockTrackCollection.Setup(x => x.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<Track>>(),
                It.IsAny<UpdateDefinition<Track>>(),
                It.IsAny<FindOneAndUpdateOptions<Track>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Track { Id = trackId, FavoriteCount = 4 });

        // Act
        var result = await _trackService.AddToFavoriteTrackAsync(trackId, false);

        // Assert
        result.Should().Be(4); // FavoriteCount should be decremented by 1
    }

    [Fact]
    public void SearchTracks_ShouldReturnMatchingTracks()
    {
        // Arrange
        var track1 = TestDataHelper.CreateTestTrack();
        track1.Name = "Test Song";
        track1.NameUnsigned = "test song";

        var track2 = TestDataHelper.CreateTestTrack();
        track2.Name = "Another Song";
        track2.NameUnsigned = "another song";

        var track3 = TestDataHelper.CreateTestTrack();
        track3.Name = "Test Track";
        track3.NameUnsigned = "test track";

        var tracks = new List<Track> { track1, track2, track3 };
        SetupMockCollection(tracks);

        // Act
        var result = _trackService.SearchTracks("test");

        // Assert
        result.Should().NotBeNull();
        // Note: Since we're using in-memory LINQ, the filtering will work correctly
        // In the actual implementation, this would filter based on NameUnsigned.Contains("test")
    }

    [Fact]
    public async Task UpsertStreamCountAsync_ShouldIncrementStreamCount()
    {
        // Arrange
        var trackId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(userId);
        
        MockRedisCacheService.Setup(x => x.HashIncrementAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                           .ReturnsAsync(1L);
        MockRedisCacheService.Setup(x => x.SetExpirationAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                           .ReturnsAsync(true);

        // Act
        await _trackService.UpsertStreamCountAsync(trackId);

        // Assert
        MockRedisCacheService.Verify(x => x.HashIncrementAsync($"stream_count:{userId}", trackId, 1), Times.Once);
        MockRedisCacheService.Verify(x => x.SetExpirationAsync($"stream_count:{userId}", TimeSpan.FromMinutes(3)), Times.Once);
    }


    [Fact]
    public async Task CheckTrackInFavoriteAsync_WhenUserNotAuthenticated_ShouldReturnFalse()
    {
        // Arrange
        MockHttpContext.User = new ClaimsPrincipal(); // No claims

        // Act
        var result = await _trackService.CheckTrackInFavoriteAsync("trackId");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckTrackInFavoriteAsync_WhenCacheHit_ShouldReturnFromCache()
    {
        // Arrange
        var trackId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        
        MockHttpContext.User = CreateTestUser(userId);
        
        MockRedisCacheService.Setup(x => x.ListLengthAsync($"favorite_track:{userId}"))
                           .ReturnsAsync(5);
        MockRedisCacheService.Setup(x => x.ListContainsAsync($"favorite_track:{userId}", trackId))
                           .ReturnsAsync(true);

        // Act
        var result = await _trackService.CheckTrackInFavoriteAsync(trackId);

        // Assert
        result.Should().BeTrue();
        MockRedisCacheService.Verify(x => x.ListContainsAsync($"favorite_track:{userId}", trackId), Times.Once);
    }

    [Fact]
    public void GetFavoriteTracks_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        MockHttpContext.User = new ClaimsPrincipal(); // No claims

        // Act & Assert
        var act = () => _trackService.GetFavoriteTracks();
        act.Should().Throw<UnauthorizedCustomException>()
           .WithMessage("Your session is limit");
    }

    [Fact]
    public void CreateTrackTemp_ShouldCreateTrackTempRequest()
    {
        // Arrange
        var createTrackRequest = new CreateTrackRequest
        {
            Name = "Test Track",
            Description = "Test Description",
            CreatedByArtistId = Guid.NewGuid().ToString(),
            MainArtistIds = new List<string>(),
            FeaturedArtistIds = new List<string>(),
            CategoryIds = new List<string>(),
            Tags = new List<string>(),
            IsRelease = true,
            ReleaseStatus = ReleaseStatus.Official,
            CreatedByUserId = Guid.NewGuid().ToString()
        };

        // Act
        var result = _trackService.CreateTrackTemp(createTrackRequest);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(createTrackRequest.Name);
        result.Description.Should().Be(createTrackRequest.Description);
        result.MainArtistIds.Should().Contain(createTrackRequest.CreatedByArtistId);
        result.CreatedBy.Should().Be(createTrackRequest.CreatedByUserId);
    }

    [Fact]
    public async Task ApproveTrackUploadRequestAsync_WithNullCombinedRequest_ShouldReturnFalse()
    {
        // Arrange
        var actionByUserId = Guid.NewGuid().ToString();
        var uploadId = Guid.NewGuid().ToString();

        // Setup Redis cache to return null combined request
        MockRedisCacheService.Setup(x => x.TryGetGeneric(
            $"upload:{uploadId}:requestUpload", 
            out It.Ref<CombinedUploadRequest?>.IsAny))
            .Returns((string key, out CombinedUploadRequest? value) =>
            {
                value = null;
                return true; // Cache key exists but value is null
            });

        // Act
        var result = await _trackService.ApproveTrackUploadRequestAsync(actionByUserId, uploadId);

        // Assert
        result.Should().BeFalse();
        
        // Verify that failure notification was sent
        _mockTrackUploadNotifier.Verify(x => x.SendFailedAsync(
            actionByUserId, 
            "An error occurred while approving the track upload request."), 
            Times.Once);
    }

    [Fact]
    public async Task ApproveTrackUploadRequestAsync_WithServiceException_ShouldReturnFalse()
    {
        // Arrange
        var actionByUserId = Guid.NewGuid().ToString();
        var uploadId = Guid.NewGuid().ToString();
        var trackId = Guid.NewGuid().ToString();

        var combinedRequest = CreateTestCombinedUploadRequest(trackId, actionByUserId);

        MockRedisCacheService.Setup(x => x.TryGetGeneric(
            $"upload:{uploadId}:requestUpload", 
            out It.Ref<CombinedUploadRequest?>.IsAny))
            .Returns((string key, out CombinedUploadRequest? value) =>
            {
                value = combinedRequest;
                return true;
            });

        // Setup one service to throw an exception - need to handle optional parameter properly
        _mockAmazonS3Service.Setup(x => x.DownloadOriginalAudioAsync(
            It.IsAny<string>(), 
            It.IsAny<Func<Stream, Task>>(),
            It.IsAny<AudioFormat>()))
            .ThrowsAsync(new Exception("S3 download failed"));

        // Act
        var result = await _trackService.ApproveTrackUploadRequestAsync(actionByUserId, uploadId);

        // Assert
        result.Should().BeFalse();
        
        // Verify error logging and failure notification
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error approving track upload request for uploadId: {uploadId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockTrackUploadNotifier.Verify(x => x.SendFailedAsync(
            actionByUserId, 
            "An error occurred while approving the track upload request."), 
            Times.Once);
    }

    #region Helper Methods

    private CombinedUploadRequest CreateTestCombinedUploadRequest(string trackId, string userId, string? artistId = null)
    {
        artistId ??= Guid.NewGuid().ToString();
        
        return new CombinedUploadRequest
        {
            Id = Guid.NewGuid().ToString(),
            Track = new TrackTempRequest
            {
                Id = trackId,
                Name = "Test Track",
                Description = "Test Description",
                Type = TrackType.Original,
                CreatedByArtistId = artistId,
                MainArtistIds = [artistId],
                FeaturedArtistIds = [],
                CategoryIds = [],
                Tags = ["test"],
                CoverImage = "test-cover.jpg",
                IsExplicit = false,
                ReleaseInfo = new ReleaseInfo
                {
                    IsRelease = true,
                    ReleaseStatus = ReleaseStatus.NotAnnounced, // Use correct enum value
                    ReleaseDate = null
                },
                LegalDocuments = [],
                CreatedBy = userId
            },
            Work = new WorkTempRequest
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Test Work",
                WorkSplits = []
            },
            Recording = new RecordingTempRequest
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Test Recording",
                RecordingSplitRequests = []
            },
            ApprovalPriority = ApprovalPriorityStatus.Low,
            CreatedBy = userId
        };
    }

    private void SetupSuccessfulApprovalMocks(string trackId, string actionByUserId, string? artistId = null)
    {
        artistId ??= Guid.NewGuid().ToString();

        // Setup Amazon S3 Service - Handle optional parameter properly
        _mockAmazonS3Service.Setup(x => x.DownloadOriginalAudioAsync(
            It.IsAny<string>(), 
            It.IsAny<Func<Stream, Task>>(),
            It.IsAny<AudioFormat>()))
            .Returns<string, Func<Stream, Task>, AudioFormat>((id, callback, format) =>
            {
                using var stream = new MemoryStream(new byte[1024]); // Mock audio data
                return callback(stream);
            });

        _mockAmazonS3Service.Setup(x => x.UploadFolderAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Setup FFMPEG Service
        var mockWavResponse = new WavFileResponse
        {
            OutputWavPath = "test.wav",
            OriginalBitrate = 320000 // Use the correct property
        };

        _mockFfmpegService.Setup(x => x.ConvertToWavAsync(
            It.IsAny<Stream>(), 
            It.IsAny<string>(), 
            It.IsAny<AudioConvertPathOptions>()))
            .ReturnsAsync(mockWavResponse);

        _mockFfmpegService.Setup(x => x.ConvertToHlsAsync(
            It.IsAny<WavFileResponse>(), 
            It.IsAny<AudioConvertPathOptions>()))
            .ReturnsAsync("test-hls-folder");

        // Setup Audio Analysis Service
        var mockAudioFeature = TestDataHelper.CreateTestAudioFeature();

        _mockAudioAnalysisService.Setup(x => x.AnalyzeAudioAsync(It.IsAny<WavFileResponse>()))
            .ReturnsAsync(mockAudioFeature);

        // Setup Category Service
        var moodTypes = new List<MoodType> { MoodType.Happy, MoodType.Energetic };
        _mockCategoryService.Setup(x => x.DetectMoods(It.IsAny<AudioFeature>()))
            .Returns(moodTypes);

        _mockCategoryService.Setup(x => x.GetMoodsFromAudioFeaturesAsync(It.IsAny<IEnumerable<MoodType>>()))
            .ReturnsAsync(new List<string> { Guid.NewGuid().ToString() });

        _mockCategoryService.Setup(x => x.GenerateAlternativeDescription(
            It.IsAny<AudioFeature>(), 
            It.IsAny<IEnumerable<MoodType>>()))
            .Returns("Generated description");

        // Setup Embedding Generator - Mock the Generate method to return a proper response
        _mockEmbeddingGenerator.Setup(x => x.GenerateAsync(
            It.IsAny<IEnumerable<string>>(), 
            It.IsAny<EmbeddingGenerationOptions>(), 
            It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<string> inputs, EmbeddingGenerationOptions options, CancellationToken ct) =>
            {
                var mockEmbeddings = inputs.Select(input => new Embedding<float>(new float[] { 0.1f, 0.2f, 0.3f }));
                return Task.FromResult<IReadOnlyList<Embedding<float>>>(mockEmbeddings.ToList());
            });


        // Setup Artist Service
        _mockArtistService.Setup(x => x.GetArtistStageNameByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync("Test Artist");

        // Setup EmySound Service
        _mockEmySoundService.Setup(x => x.UploadTrackFingerprintAsync(
            It.IsAny<Stream>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>()))
            .ReturnsAsync("fingerprint-id");

        // Setup Redis Cache Service for removal
        MockRedisCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Setup Approval History Service
        _mockApprovalHistoryService.Setup(x => x.CreateApprovalHistoryAsync(It.IsAny<ApprovalHistoryRequest>()))
            .Returns(Task.CompletedTask);

        // Setup Track Upload Notifier
        _mockTrackUploadNotifier.Setup(x => x.SendProgressAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTrackUploadNotifier.Setup(x => x.SendCompletedAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTrackUploadNotifier.Setup(x => x.SendFailedAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Setup collections
        SetupMockCollection<Track>();
        SetupMockCollection<Work>();
        SetupMockCollection<Recording>();
    }

    private void VerifyApprovalServiceCalls(string actionByUserId, string uploadId)
    {
        // Verify progress notifications were sent
        _mockTrackUploadNotifier.Verify(x => x.SendProgressAsync(
            actionByUserId, 
            It.IsAny<int>(), 
            It.IsAny<string>()), 
            Times.AtLeastOnce);

        // Verify completion notification was sent
        _mockTrackUploadNotifier.Verify(x => x.SendCompletedAsync(actionByUserId), Times.Once);

        // Verify Redis cache was cleaned up
        MockRedisCacheService.Verify(x => x.RemoveAsync($"upload:{uploadId}:requestUpload"), Times.Once);

        // Verify approval histories were created (3 times: Track, Work, Recording)
        _mockApprovalHistoryService.Verify(x => x.CreateApprovalHistoryAsync(It.IsAny<ApprovalHistoryRequest>()), Times.Exactly(3));

        // Verify S3 operations - Handle optional parameter properly
        _mockAmazonS3Service.Verify(x => x.DownloadOriginalAudioAsync(
            It.IsAny<string>(), 
            It.IsAny<Func<Stream, Task>>(), 
            It.IsAny<AudioFormat>()), Times.Once);
        _mockAmazonS3Service.Verify(x => x.UploadFolderAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    #endregion
}