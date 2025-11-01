using AutoMapper;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Tracks;

public sealed class TrackService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, ILogger<TrackService> logger) : ITrackService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;
    private readonly ILogger<TrackService> _logger = logger;

    public async Task SeedMonthlyStreamCountByTrackIdAsync(string trackId, long streamCount, int month, int year)
    {
        UpdateResult updateTrackResult = await _unitOfWork.GetCollection<Track>()
            .UpdateOneAsync(x => x.Id == trackId, Builders<Track>.Update.Set(x => x.StreamCount, streamCount));

        if (updateTrackResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Cannot seed stream count.");
        }

        await _unitOfWork.GetCollection<MonthlyStreamCount>().InsertOneAsync(new MonthlyStreamCount
        {
            TrackId = trackId,
            StreamCount = streamCount,
            Month = month,
            Year = year,
        });
    }

    public IQueryable<Track> GetTracks()
    {
        return _unitOfWork.GetCollection<Track>().AsQueryable();
    }

    public IQueryable<Track> GetFavoriteTracks()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        List<string> favoriteTrackIds = _unitOfWork.GetCollection<UserEngagement>()
            .Find(x => x.ActorId == userId && x.TargetType == UserEngagementTargetType.Track && x.Action == UserEngagementAction.Like)
            .Project(x => x.TargetId)
            .ToList();

        IQueryable<Track> query = _unitOfWork.GetCollection<Track>()
            .Find(x => favoriteTrackIds.Contains(x.Id))
            .ToEnumerable()
            .AsQueryable();

        return query;
    }

    public IQueryable<Track> SearchTracks(string name)
    {
        IQueryable<Track> query = _unitOfWork.GetCollection<Track>().AsQueryable();

        if (string.IsNullOrEmpty(name))
        {
            return query;
        }

        string unsignedSearchTerm = HelperMethod.ToUnsigned(name);
        query = query.Where(t => t.NameUnsigned.Contains(unsignedSearchTerm));

        return query;
    }

    public async Task ReleaseScheduledTrackAsync(string trackId)
    {
        DateTimeOffset currentTime = HelperMethod.GetUtcPlus7TimeOffset();

        // Cập nhật thông tin phát hành track với điều kiện track chưa được release
        UpdateDefinition<Track> updateDefinition = Builders<Track>.Update
            .Set(t => t.ReleaseInfo.ReleasedAt, currentTime)
            .Set(t => t.ReleaseInfo.ReleaseStatus, ReleaseStatus.Official);

        FilterDefinition<Track> filter = Builders<Track>.Filter.And(
            Builders<Track>.Filter.Eq(t => t.Id, trackId),
            Builders<Track>.Filter.Eq(t => t.ReleaseInfo.ReleaseStatus, ReleaseStatus.NotAnnounced)
        );

        UpdateResult result = await _unitOfWork.GetCollection<Track>()
            .UpdateOneAsync(filter, updateDefinition);

        if (result.MatchedCount == 0)
        {
            throw new UnprocessableEntityCustomException($"Track {trackId} not found or already released. Skipping release job.");
        }

        if (result.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException($"Track {trackId} was not modified. It may have been released by another process.");
        }
    }

    public async Task CreateTrackFromTrackUploadRequestAsync(TrackTempResponse trackResponse, WorkTempRequest workTempRequest, RecordingTempRequest recordingTempRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            Track track = new()
            {
                Id = trackResponse.Id,
                Name = trackResponse.Name,
                NameUnsigned = HelperMethod.ToUnsigned(trackResponse.Name),
                Description = trackResponse.Description,

                Type = trackResponse.Type,

                MainArtistIds = trackResponse.MainArtistIds,
                FeaturedArtistIds = trackResponse.FeaturedArtistIds,
                CategoryIds = trackResponse.CategoryIds,
                Tags = trackResponse.Tags,

                CoverImage = trackResponse.CoverImage,
                PreviewVideo = trackResponse.PreviewVideo,

                //AudioFingerprint = trackResponse.AudioFingerprint,
                AudioFeature = trackResponse.AudioFeature,
                AlternativeDescription = trackResponse.AlternativeDescription,
                EmbeddingVector = trackResponse.EmbeddingVector,

                IsExplicit = trackResponse.IsExplicit,
                Lyrics = trackResponse.Lyrics,

                ReleaseInfo = trackResponse.ReleaseInfo,
                Restriction = new()
                {
                    Type = RestrictionType.None,
                },

                LegalDocuments = trackResponse.LegalDocuments,

                CreatedBy = trackResponse.CreatedBy,
            };

            Work work = new()
            {
                Id = workTempRequest.Id,
                TrackId = trackResponse.Id,

                Description = workTempRequest.Description,
                WorkSplits = _mapper.Map<List<WorkSplit>>(workTempRequest.WorkSplits),
                Version = 1,
                Status = WorkStatus.Active,
            };

            Recording recording = new()
            {
                Id = recordingTempRequest.Id,
                TrackId = trackResponse.Id,

                Description = recordingTempRequest.Description,
                RecordingSplits = _mapper.Map<List<RecordingSplit>>(recordingTempRequest.RecordingSplitRequests),
                Version = 1,
                Status = RecordingStatus.Active,
            };

            await _unitOfWork.GetCollection<Track>().InsertOneAsync(session, track);
            await _unitOfWork.GetCollection<Work>().InsertOneAsync(session, work);
            await _unitOfWork.GetCollection<Recording>().InsertOneAsync(session, recording);
        });
    }

    public TrackTempRequest CreateTrackTemp(CreateTrackRequest createTrackRequest)
    {
        string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Workaround for tránh trùng userId khi tạo track
        createTrackRequest.MainArtistIds.Add(artistId);
        createTrackRequest.MainArtistIds = createTrackRequest.MainArtistIds.Distinct().ToList();

        TrackTempRequest track = new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = createTrackRequest.Name,
            Description = createTrackRequest.Description,

            MainArtistIds = createTrackRequest.MainArtistIds,
            FeaturedArtistIds = createTrackRequest.FeaturedArtistIds,
            CategoryIds = createTrackRequest.CategoryIds,
            Tags = createTrackRequest.Tags,

            CoverImage = createTrackRequest.CoverImage,
            PreviewVideo = createTrackRequest.PreviewVideo,
            IsExplicit = createTrackRequest.IsExplicit,
            Lyrics = createTrackRequest.Lyrics,

            ReleaseInfo = new()
            {
                IsRelease = createTrackRequest.IsReleased,
                ReleaseDate = createTrackRequest.ReleaseDate,
                ReleaseStatus = createTrackRequest.ReleaseStatus,
            },

            LegalDocuments = createTrackRequest.LegalDocuments,

            CreatedBy = artistId,
        };

        return track;
    }

    public async Task<PaginatedData<CombinedUploadRequest>> GetPendingTrackUploadRequestsAsync(int pageNumber = 1, int pageSize = 20)
    {
        ICacheResult<PaginatedData<CombinedUploadRequest>> result = await _redisCacheService.GetPendingCombinedUploadsAsync(pageNumber, pageSize);

        PaginatedData<CombinedUploadRequest> paginatedData;

        if (!result.Success || result.Value == null)
        {
            return new PaginatedData<CombinedUploadRequest>
            {
                Items = [],
                TotalCount = 0
            };
        }

        paginatedData = new()
        {
            Items = result.Value.Items,
            TotalCount = result.Value.TotalCount
        };

        return paginatedData;
    }

    public async Task<CombinedUploadRequest> GetPendingTrackUploadRequestByIdAsync(string uploadId)
    {
        ICacheResult<CombinedUploadRequest> cacheResult = await _redisCacheService.TryGetGenericAsync<CombinedUploadRequest>($"upload:{uploadId}:requestUpload");

        if (!cacheResult.Success || cacheResult.Value == null)
        {
            throw new NotFoundCustomException($"Upload request with ID {uploadId} not found or expired.");
        }

        return cacheResult.Value;
    }

    #region Favorite Tracks
    public async Task<long> AddToFavoriteTrackAsync(string trackId, bool isAdding)
    {
        Track trackUpdated = await _unitOfWork.GetCollection<Track>()
        .FindOneAndUpdateAsync(t => t.Id == trackId, Builders<Track>.Update.Inc(t => t.FavoriteCount, 1),
        new FindOneAndUpdateOptions<Track>
        {
            // Trả về tài liệu sau khi cập nhật
            ReturnDocument = ReturnDocument.After,
            Projection = Builders<Track>.Projection
                .Include(t => t.Id)
                .Include(t => t.FavoriteCount)
        }) ?? throw new NotFoundCustomException($"Track with ID {trackId} not found.");

        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string role = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Nếu isAdding false, tức là người dùng bỏ thích bài hát
        if (!isAdding)
        {
            // Xóa track khỏi cache yêu thích của users
            await RemoveTrackFromFavoriteCacheAsync(userId, trackId);
            return trackUpdated.FavoriteCount;
        }

        // Thêm track yêu thích của users vào UserEngagement
        await _unitOfWork.GetCollection<UserEngagement>()
            .InsertOneAsync(new UserEngagement
            {
                ActorId = userId,
                ActorType = Enum.Parse<UserRole>(role) == UserRole.Listener ? UserEngagementTargetType.Listener : UserEngagementTargetType.Artist,
                TargetId = trackId,
                TargetType = UserEngagementTargetType.Track,
                Action = UserEngagementAction.Like,
            });

        // Thêm track yêu thích của users vào cache
        await AddTrackToFavoriteCacheAsync(userId, trackId);

        // Trả về số lượt yêu thích mới của bài hát
        return trackUpdated.FavoriteCount;
    }

    public async Task<bool> CheckTrackInFavoriteAsync(string trackId)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        try
        {
            string cacheKey = $"favorite_track:{userId}";

            // Check if cache exists and has items
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache hit - check if track exists in Redis list
                return await _redisCacheService.ListContainsAsync(cacheKey, trackId);
            }

            // Cache miss - populate from database
            await EnsureCachePopulatedAsync(userId);

            // Check again after population
            return await _redisCacheService.ListContainsAsync(cacheKey, trackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if track {TrackId} is in favorite for user {UserId}", trackId, userId);
            return false;
        }
    }

    private async Task AddTrackToFavoriteCacheAsync(string userId, string trackId)
    {
        try
        {
            string cacheKey = $"favorite_track:{userId}";

            // Check if cache exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache exists - check if track already exists to avoid duplicates
                bool exists = await _redisCacheService.ListContainsAsync(cacheKey, trackId);

                if (!exists)
                {
                    // Get current TTL to preserve it
                    var remainingTtl = await _redisCacheService.GetTTLAsync(cacheKey);
                    var ttlToSet = remainingTtl ?? TimeSpan.FromHours(1);

                    // Add track to the beginning of the list
                    await _redisCacheService.ListPushAsync(cacheKey, trackId, ttlToSet);
                }
            }
            else
            {
                // Cache doesn't exist - create new list with this track
                await _redisCacheService.ListPushAsync(cacheKey, trackId, TimeSpan.FromHours(1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add track {TrackId} to favorite cache for user {UserId}", trackId, userId);
        }
    }

    private async Task RemoveTrackFromFavoriteCacheAsync(string userId, string trackId)
    {
        try
        {
            string cacheKey = $"favorite_track:{userId}";

            // Check if cache exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Get current TTL to preserve it
                var remainingTtl = await _redisCacheService.GetTTLAsync(cacheKey);

                // Remove track from list (removes all occurrences)
                long removedCount = await _redisCacheService.ListRemoveAsync(cacheKey, trackId, 0);

                if (removedCount > 0)
                {
                    // Restore TTL if there are still items in the list
                    long newLength = await _redisCacheService.ListLengthAsync(cacheKey);
                    if (newLength > 0 && remainingTtl.HasValue)
                    {
                        await _redisCacheService.SetExpirationAsync(cacheKey, remainingTtl);
                    }

                    _logger.LogDebug("Removed track {TrackId} from favorite cache for user {UserId}", trackId, userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove track {TrackId} from favorite cache for user {UserId}", trackId, userId);
        }
    }

    private async Task InvalidateFavoriteCacheAsync(string userId)
    {
        try
        {
            string cacheKey = $"favorite_track:{userId}";
            await _redisCacheService.RemoveAsync(cacheKey);

            _logger.LogDebug("Invalidated favorite cache for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate favorite cache for user {UserId}", userId);
        }
    }

    private async Task<bool> EnsureCachePopulatedAsync(string userId)
    {
        try
        {
            string cacheKey = $"favorite_track:{userId}";

            // Check if cache already exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);
            if (listLength > 0)
            {
                return true; // Cache already populated
            }

            // Fetch favorite track from database
            List<string> favoriteTrackIds = await _unitOfWork.GetCollection<UserEngagement>()
                .Find(x => x.ActorId == userId && x.TargetType == UserEngagementTargetType.Track && x.Action == UserEngagementAction.Like)
                .Project(x => x.TargetId)
                .ToListAsync();

            if (favoriteTrackIds.Count > 0)
            {
                // Populate cache with track IDs
                await _redisCacheService.ListPushRangeAsync(cacheKey, favoriteTrackIds, TimeSpan.FromHours(1));

                _logger.LogDebug("Populated favorite cache for user {UserId} with {Count} tracks", userId, favoriteTrackIds.Count);
                return true;
            }

            _logger.LogDebug("No favorite tracks found for user {UserId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to populate favorite cache for user {UserId}", userId);
            return false;
        }
    }
    #endregion

    public async Task UpdateStreamCount(string trackId)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string key = $"stream_count:{userId}";  //--> cái cũ là top track ??
        //tăng lượt stream count lên 1 khi được gọi
        await _redisCacheService.HashIncrementAsync(key, trackId);
        //set thời gian tồn tại của key trong 30'
        await _redisCacheService.SetExpirationAsync(key, TimeSpan.FromMinutes(3));
    }

    //NOTE: Hàm để chạy duyệt qua các track chưa có embedding chứ ko phải 1 track
    public async Task AddEmbeddingVectorAsync()
    {
        try
        {
            IEnumerable<Track> tracks = await _unitOfWork.GetCollection<Track>()
            .Find(t => t.Description != null)
            .ToListAsync();

            //lọc ra các track chưa có embedding
            var trackWithoutEmbedding = tracks
                                        .Where(t => t.EmbeddingVector is null or { Length: 0 })
                                        .ToList();


            var embedding = new Dictionary<string, float[]>();

            //lặp qua các track chưa có embedding và tạo vector cho từng track
            foreach (var track in trackWithoutEmbedding)
            {
                //nối description và alternative description
                string totalDescription = (track.Description ?? string.Empty) + ". " + track.AlternativeDescription;
                if (!embedding.ContainsKey(totalDescription))
                {
                    embedding[track.Id] = await GenerateEmbeddingsAsync(totalDescription);
                }
            }

            //update tất cả các track chưa có embedding
            var updates = new List<UpdateOneModel<Track>>();
            foreach (var track in trackWithoutEmbedding)
            {
                var filter = Builders<Track>.Filter.Eq(t => t.Id, track.Id);
                var update = Builders<Track>.Update.Set(t => t.EmbeddingVector, embedding[track.Id]);
                updates.Add(new UpdateOneModel<Track>(filter, update));

            }

            if (updates.Any())
            {
                await _unitOfWork.GetCollection<Track>().BulkWriteAsync(updates);
            }
        }
        catch (Exception e)
        {
            throw new BadRequestCustomException(e.Message);
        }

    }

    public async Task<float[]> GenerateEmbeddingsAsync(string term)
    {
        var generatedEmbeddings = await _embeddingGenerator.GenerateAsync([term]);
        var embedding = generatedEmbeddings.Single();
        return embedding.Vector.ToArray();
    }

    //NOTE: Hàm tìm kiếm track theo semantic
    public async Task<IEnumerable<Track>> GetAllTracksBySemanticAsync(string text, int limit = 20)
    {
        //nếu text rỗng thì trả về track nhu bình thường
        if (string.IsNullOrEmpty(text))
        {
            return GetTracks();
        }

        //tạo vector từ text để tí so sánh
        var embedding = await GenerateEmbeddingsAsync(text);

        var vectorSearchOptions = new VectorSearchOptions<Track>
        {
            IndexName = "vector_index",
            //lấy 150 vector gần giống để so sánh
            NumberOfCandidates = 150,
        };

        return await _unitOfWork.GetCollection<Track>()
            .Aggregate()
            .VectorSearch(track => track.EmbeddingVector, embedding, limit, vectorSearchOptions)
            .Project<Track>(Builders<Track>.Projection
                .Exclude(t => t.EmbeddingVector)
                .Exclude(t => t.AudioFeature)
                .Exclude(t => t.AlternativeDescription))
            .ToListAsync();
    }

    public async Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id)
    {
        Track track = await _unitOfWork.GetCollection<Track>()
            .Find(x => x.Id == id)
            .Project<Track>(projection)
            .FirstOrDefaultAsync();

        return _mapper.Map<TrackResponse>(track);
    }
}
