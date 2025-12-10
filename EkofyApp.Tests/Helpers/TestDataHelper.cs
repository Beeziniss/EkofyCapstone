using MongoDB.Driver;
using System.Linq.Expressions;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Tests.Helpers;

public static class TestDataHelper
{
    public static User CreateTestUser(
        string? id = null,
        string email = "test@example.com",
        UserRole role = UserRole.Listener,
        UserStatus status = UserStatus.Active)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Email = email,
            PasswordHash = "hashed_password",
            FullName = "Test User",
            Role = role,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            Restrictions = new List<Restriction>()
        };
    }

    public static Artist CreateTestArtist(string? id = null, string? userId = null)
    {
        return new Artist
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserId = userId ?? Guid.NewGuid().ToString(),
            StageName = "Test Artist",
            StageNameUnsigned = "test artist", // Add the unsigned version
            Email = "artist@example.com",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Listener CreateTestListener(string? id = null, string? userId = null)
    {
        return new Listener
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserId = userId ?? Guid.NewGuid().ToString(),
            DisplayName = "Test Listener",
            DisplayNameUnsigned = "test listener", // Add the unsigned version
            Email = "listener@example.com",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Track CreateTestTrack(string? id = null, List<string>? mainArtistIds = null)
    {
        return new Track
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = "Test Track",
            NameUnsigned = "test track", // Add the unsigned version
            MainArtistIds = mainArtistIds ?? [Guid.NewGuid().ToString()],
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Playlist CreateTestPlaylist(string? id = null, string? userId = null)
    {
        return new Playlist
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserId = userId ?? Guid.NewGuid().ToString(),
            Name = "Test Playlist",
            IsPublic = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Category CreateTestCategory(string? id = null, CategoryType type = CategoryType.Genre)
    {
        return new Category
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = "Test Category",
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Subscription CreateTestSubscription(string? id = null)
    {
        return new Subscription
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = "Test Subscription",
            Code = "TEST_SUB",
            Amount = 10.99m,
            Tier = SubscriptionTier.Free,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static CombinedUploadRequest CreateTestCombinedUploadRequest(
        string? id = null,
        string? trackId = null,
        string? userId = null,
        string? artistId = null)
    {
        trackId ??= Guid.NewGuid().ToString();
        userId ??= Guid.NewGuid().ToString();
        artistId ??= Guid.NewGuid().ToString();

        return new CombinedUploadRequest
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Track = CreateTestTrackTempRequest(trackId, userId, artistId),
            Work = CreateTestWorkTempRequest(),
            Recording = CreateTestRecordingTempRequest(),
            ApprovalPriority = ApprovalPriorityStatus.Low,
            CreatedBy = userId
        };
    }

    public static TrackTempRequest CreateTestTrackTempRequest(
        string? id = null,
        string? userId = null,
        string? artistId = null)
    {
        id ??= Guid.NewGuid().ToString();
        userId ??= Guid.NewGuid().ToString();
        artistId ??= Guid.NewGuid().ToString();

        return new TrackTempRequest
        {
            Id = id,
            Name = "Test Track Upload",
            Description = "Test track description",
            Type = TrackType.Original,
            CreatedByArtistId = artistId,
            MainArtistIds = [artistId],
            FeaturedArtistIds = [],
            CategoryIds = [Guid.NewGuid().ToString()],
            Tags = ["test", "upload"],
            CoverImage = "test-cover.jpg",
            PreviewVideo = null,
            IsExplicit = false,
            Lyrics = "Test lyrics",
            ReleaseInfo = new ReleaseInfo
            {
                IsRelease = true,
                ReleaseStatus = ReleaseStatus.NotAnnounced, // Use correct enum value
                ReleaseDate = null
            },
            LegalDocuments = [],
            CreatedBy = userId
        };
    }

    public static WorkTempRequest CreateTestWorkTempRequest(string? id = null)
    {
        return new WorkTempRequest
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Description = "Test work description",
            WorkSplits = []
        };
    }

    public static RecordingTempRequest CreateTestRecordingTempRequest(string? id = null)
    {
        return new RecordingTempRequest
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Description = "Test recording description",
            RecordingSplitRequests = []
        };
    }

    public static AudioFeature CreateTestAudioFeature()
    {
        return new AudioFeature
        {
            Tempo = 120.0f,
            Key = "C",
            KeyNumber = 0,
            Mode = "Major",
            ModeNumber = 1,
            Energy = 0.8f,
            Danceability = 0.7f,
            Acousticness = 0.3f,
            SpectralCentroid = 1500.0f,
            ZeroCrossingRate = 0.1f,
            Duration = 180.0f, // 3 minutes in seconds
            ChromaMean = [0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 0.8f, 0.6f],
            MfccMean = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f]
        };
    }

    public static Work CreateTestWork(string? id = null, string? trackId = null)
    {
        return new Work
        {
            Id = id ?? Guid.NewGuid().ToString(),
            TrackId = trackId ?? Guid.NewGuid().ToString(),
            Description = "Test work",
            WorkSplits = [],
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Recording CreateTestRecording(string? id = null, string? trackId = null)
    {
        return new Recording
        {
            Id = id ?? Guid.NewGuid().ToString(),
            TrackId = trackId ?? Guid.NewGuid().ToString(),
            Description = "Test recording",
            RecordingSplits = [],
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}