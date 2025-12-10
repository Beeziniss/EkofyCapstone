using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.ApprovalHistories;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ServiceInterfaces.ApprovalHistories;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Application.ServiceInterfaces.Recommendations;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Application.ThirdPartyServiceInterfaces.EmySound;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services.Notifications;
using EkofyApp.Infrastructure.Services.Tracks;
using EkofyApp.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using HotChocolate.Subscriptions;

namespace EkofyApp.Tests.Services;

public class TrackServiceApprovalTests : BaseServiceTest
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

    public TrackServiceApprovalTests()
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
    public async Task ApproveTrackUploadRequestAsync_WithValidUploadId_ShouldReturnTrue()
    {
        // Arrange
        var actionByUserId = Guid.NewGuid().ToString();
        var uploadId = Guid.NewGuid().ToString();
        var trackId = Guid.NewGuid().ToString();
        var artistId = Guid.NewGuid().ToString();

        var combinedRequest = CreateTestCombinedUploadRequest(trackId, actionByUserId, artistId);

        // Setup Redis cache to return the combined request
        MockRedisCacheService.Setup(x => x.TryGetGeneric(
            $"upload:{uploadId}:requestUpload", 
            out It.Ref<CombinedUploadRequest?>.IsAny))
            .Returns((string key, out CombinedUploadRequest? value) =>
            {
                value = combinedRequest;
                return true;
            });

        // Setup all required service mocks
        SetupSuccessfulApprovalMocks(trackId, actionByUserId, artistId);

        // Act
        var result = await _trackService.ApproveTrackUploadRequestAsync(actionByUserId, uploadId);

        // Assert
        result.Should().BeTrue();
        
        // Verify that key services were called
        VerifyApprovalServiceCalls(actionByUserId, uploadId);
    }

    [Fact]
    public async Task ApproveTrackUploadRequestAsync_WithNonExistentUploadId_ShouldReturnFalse()
    {
        // Arrange
        var actionByUserId = Guid.NewGuid().ToString();
        var uploadId = Guid.NewGuid().ToString();

        // Setup Redis cache to return null (upload not found)
        MockRedisCacheService.Setup(x => x.TryGetGeneric(
            $"upload:{uploadId}:requestUpload", 
            out It.Ref<CombinedUploadRequest?>.IsAny))
            .Returns((string key, out CombinedUploadRequest? value) =>
            {
                value = null;
                return false;
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
    public async Task ApproveTrackUploadRequestAsync_WithOfficialReleaseStatus_ShouldSetReleaseDate()
    {
        // Arrange
        var actionByUserId = Guid.NewGuid().ToString();
        var uploadId = Guid.NewGuid().ToString();
        var trackId = Guid.NewGuid().ToString();
        var artistId = Guid.NewGuid().ToString();

        var combinedRequest = CreateTestCombinedUploadRequest(trackId, actionByUserId, artistId);
        combinedRequest.Track.ReleaseInfo.ReleaseStatus = ReleaseStatus.Official;

        // Setup Redis cache
        MockRedisCacheService.Setup(x => x.TryGetGeneric(
            $"upload:{uploadId}:requestUpload", 
            out It.Ref<CombinedUploadRequest?>.IsAny))
            .Returns((string key, out CombinedUploadRequest? value) =>
            {
                value = combinedRequest;
                return true;
            });

        // Setup all required service mocks
        SetupSuccessfulApprovalMocks(trackId, actionByUserId, artistId);

        // Act
        var result = await _trackService.ApproveTrackUploadRequestAsync(actionByUserId, uploadId);

        // Assert
        result.Should().BeTrue();
        
        // The release date should be set when status is Official
        combinedRequest.Track.ReleaseInfo.ReleaseDate.Should().NotBeNull();
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

        // Setup one service to throw an exception
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
                    ReleaseStatus = ReleaseStatus.NotAnnounced,
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

        // Setup Amazon S3 Service
        _mockAmazonS3Service.Setup(x => x.DownloadOriginalAudioAsync(
            It.IsAny<string>(), 
            It.IsAny<Func<Stream, Task>>(),
            It.IsAny<AudioFormat>()))
            .Returns<string, Func<Stream, Task>, AudioFormat>((id, callback, format) =>
            {
                using var stream = new MemoryStream(new byte[1024]);
                return callback(stream);
            });

        _mockAmazonS3Service.Setup(x => x.UploadFolderAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Setup FFMPEG Service
        var mockWavResponse = new WavFileResponse
        {
            OutputWavPath = "test.wav",
            OriginalBitrate = 320000
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

        // Setup Embedding Generator
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

        // Verify S3 operations
        _mockAmazonS3Service.Verify(x => x.DownloadOriginalAudioAsync(
            It.IsAny<string>(), 
            It.IsAny<Func<Stream, Task>>(), 
            It.IsAny<AudioFormat>()), Times.Once);
        _mockAmazonS3Service.Verify(x => x.UploadFolderAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    #endregion
}