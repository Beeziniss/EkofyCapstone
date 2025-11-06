using EkofyApp.Application.Models.Playlists;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Playlists;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Playlists;
public sealed class PlaylistService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService, ILogger<PlaylistService> logger) : IPlaylistService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly ILogger<PlaylistService> _logger = logger;

    public IQueryable<Playlist> GetPlaylists()
    {
        return _unitOfWork.GetCollection<Playlist>().AsQueryable();
    }

    public IQueryable<Playlist> GetFavoritePlaylists()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        List<string> favoritePlaylistIds = _unitOfWork.GetCollection<UserEngagement>()
            .Find(x => x.ActorId == userId && x.TargetType == UserEngagementTargetType.Playlist && x.Action == UserEngagementAction.Like)
            .Project(x => x.TargetId)
            .ToList();

        IQueryable<Playlist> query = _unitOfWork.GetCollection<Playlist>()
            .Find(x => favoritePlaylistIds.Contains(x.Id))
            .ToEnumerable()
            .AsQueryable();

        return query;
    }

    public IQueryable<Playlist> SearchPlaylists(string name)
    {
        IQueryable<Playlist> query = _unitOfWork.GetCollection<Playlist>().AsQueryable();

        if (string.IsNullOrEmpty(name))
        {
            return query;
        }

        string unsignedSearchTerm = HelperMethod.ToUnsigned(name);
        query = query.Where(t => t.NameUnsigned.Contains(unsignedSearchTerm));

        return query;
    }

    public async Task CreatePlaylistAsync(CreatePlaylistRequest createPlaylistRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        await _unitOfWork.GetCollection<Playlist>().InsertOneAsync(new Playlist()
        {
            UserId = userId,
            Name = createPlaylistRequest.Name,
            NameUnsigned = HelperMethod.ToUnsigned(createPlaylistRequest.Name),
            Description = createPlaylistRequest.Description,
            CoverImage = createPlaylistRequest.CoverImage,
            IsPublic = createPlaylistRequest.IsPublic,
        });
    }

    public async Task UpdatePlaylistAsync(UpdatePlaylistRequest updatePlaylistRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Build update definition based on provided fields
        UpdateDefinitionBuilder<Playlist> updateDefinitionBuilder = Builders<Playlist>.Update;
        List<UpdateDefinition<Playlist>> updates = [];

        if (!string.IsNullOrEmpty(updatePlaylistRequest.Name))
        {
            updates.Add(updateDefinitionBuilder.Set(x => x.Name, updatePlaylistRequest.Name));
            updates.Add(updateDefinitionBuilder.Set(x => x.NameUnsigned, HelperMethod.ToUnsigned(updatePlaylistRequest.Name)));
        }

        if (updatePlaylistRequest.Description != null)
        {
            updates.Add(updateDefinitionBuilder.Set(x => x.Description, updatePlaylistRequest.Description));
        }

        if (updatePlaylistRequest.CoverImage != null)
        {
            updates.Add(updateDefinitionBuilder.Set(x => x.CoverImage, updatePlaylistRequest.CoverImage));
        }

        if (updatePlaylistRequest.IsPublic.HasValue)
        {
            updates.Add(updateDefinitionBuilder.Set(x => x.IsPublic, updatePlaylistRequest.IsPublic.Value));
        }

        updates.Add(updateDefinitionBuilder.Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));

        if (updates.Count == 1) // Only UpdatedAt
        {
            throw new BadRequestCustomException("No fields to update.");
        }

        UpdateDefinition<Playlist> updateDefinition = updateDefinitionBuilder.Combine(updates);

        // Update only if the playlist belongs to the user
        UpdateResult updateResult = await _unitOfWork.GetCollection<Playlist>()
            .UpdateOneAsync(x => x.Id == updatePlaylistRequest.PlaylistId && x.UserId == userId, updateDefinition);

        if (updateResult.MatchedCount == 0)
        {
            throw new NotFoundCustomException("Playlist not found");
        }

        if (updateResult.ModifiedCount == 0)
        {
            throw new UnprocessableEntityCustomException("Cannot update playlist");
        }
    }

    public async Task AddToPlaylistAsync(AddToPlaylistRequest addToPlaylistRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        Playlist? playlist = await _unitOfWork.GetCollection<Playlist>()
            .Find(x => x.Id == addToPlaylistRequest.PlaylistId)
            .Project<Playlist>(Builders<Playlist>.Projection
                .Include(x => x.Id)
                .Include(x => x.TracksInfo))
            .FirstOrDefaultAsync();

        if (playlist == null)
        {
            await _unitOfWork.GetCollection<Playlist>().InsertOneAsync(new Playlist()
            {
                UserId = userId,
                Name = addToPlaylistRequest.PlaylistName!,
                NameUnsigned = HelperMethod.ToUnsigned(addToPlaylistRequest.PlaylistName!),
                TracksInfo =
                [
                    new PlaylistTracksInfo
                    {
                        TrackId = addToPlaylistRequest.TrackId,
                        AddedTime = HelperMethod.GetUtcPlus7TimeOffset(),
                    }
                ]
            });

            return;
        }

        if (playlist.TracksInfo.Any(x => x.TrackId == addToPlaylistRequest.TrackId))
        {
            throw new BadRequestCustomException("Track already added in the playlist.");
        }

        UpdateDefinition<Playlist> updateDefinition = Builders<Playlist>.Update
            .Push(x => x.TracksInfo, new PlaylistTracksInfo
            {
                TrackId = addToPlaylistRequest.TrackId,
                AddedTime = HelperMethod.GetUtcPlus7TimeOffset(),
            });
        UpdateResult updateResult = await _unitOfWork.GetCollection<Playlist>()
            .UpdateOneAsync(x => x.Id == playlist.Id, updateDefinition);
    }

    public async Task AddToFavoritePlaylistAsync(string playlistId, bool isAdding)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string role = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Nếu isAdding false, tức là người dùng bỏ thích playlist
        if (!isAdding)
        {
            // Xóa track khỏi cache yêu thích của users
            await RemovePlaylistFromFavoriteCacheAsync(userId, playlistId);

            return;
        }

        // Thêm track yêu thích của users vào UserEngagement
        await _unitOfWork.GetCollection<UserEngagement>()
            .InsertOneAsync(new UserEngagement
            {
                ActorId = userId,
                ActorType = Enum.Parse<UserRole>(role) == UserRole.Listener ? UserEngagementTargetType.Listener : UserEngagementTargetType.Artist,
                TargetId = playlistId,
                TargetType = UserEngagementTargetType.Playlist,
                Action = UserEngagementAction.Like,
            });

        // Thêm track yêu thích của users vào cache
        await AddPlaylistToFavoriteCacheAsync(userId, playlistId);
    }

    public async Task RemoveFromPlaylistAsync(RemoveFromPlaylistRequest removeFromPlaylistRequest)
    {
        UpdateDefinition<Playlist> updateDefinition = Builders<Playlist>.Update
            .PullFilter(x => x.TracksInfo, y => y.TrackId == removeFromPlaylistRequest.TrackId);
        UpdateResult updateResult = await _unitOfWork.GetCollection<Playlist>()
            .UpdateOneAsync(x => x.Id == removeFromPlaylistRequest.PlaylistId, updateDefinition);

        if (updateResult.ModifiedCount == 0)
        {
            throw new BadRequestCustomException("Track does not exist in the playlist.");
        }
    }

    public async Task DeletePlaylistAsync(string playlistId)
    {
        if(await _unitOfWork.GetCollection<Playlist>()
            .Find(x => x.Id == playlistId)
            .Project(x => x.Name)
            .FirstOrDefaultAsync() == "Favorite Songs")
        {
            throw new BadRequestCustomException("Cannot delete favorite songs playlist.");
        }

        DeleteResult deleteResult = await _unitOfWork.GetCollection<Playlist>()
            .DeleteOneAsync(x => x.Id == playlistId);

        if (deleteResult.DeletedCount == 0)
        {
            throw new NotFoundCustomException("Playlist does not exist.");
        }
    }

    #region Caching
    public async Task<bool> CheckPlaylistInFavoriteAsync(string playlistId)
    {
        //string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        string? userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        try
        {
            string cacheKey = $"favorite_playlist:{userId}";

            // Check if cache exists and has items
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache hit - check if track exists in Redis list
                return await _redisCacheService.ListContainsAsync(cacheKey, playlistId);
            }

            // Cache miss - populate from database
            await EnsureCachePopulatedAsync(userId);

            // Check again after population
            return await _redisCacheService.ListContainsAsync(cacheKey, playlistId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if track {PlaylistId} is in favorite for user {UserId}", playlistId, userId);
            return false;
        }
    }

    private async Task<bool> EnsureCachePopulatedAsync(string userId)
    {
        try
        {
            string cacheKey = $"favorite_playlist:{userId}";

            // Check if cache already exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);
            if (listLength > 0)
            {
                return true; // Cache already populated
            }

            // Fetch favorite playlist from database
            List<string> favoritePlaylistIds = await _unitOfWork.GetCollection<UserEngagement>()
                .Find(x => x.ActorId == userId && x.TargetType == UserEngagementTargetType.Playlist && x.Action == UserEngagementAction.Like)
                .Project(x => x.TargetId)
                .ToListAsync();

            if (favoritePlaylistIds.Count > 0)
            {
                // Populate cache with track IDs
                await _redisCacheService.ListPushRangeAsync(cacheKey, favoritePlaylistIds, TimeSpan.FromHours(1));

                _logger.LogDebug("Populated favorite cache for user {UserId} with {Count} playlists", userId, favoritePlaylistIds.Count);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to populate favorite cache for user {UserId}", userId);
            return false;
        }
    }

    private async Task AddPlaylistToFavoriteCacheAsync(string userId, string playlistId)
    {
        try
        {
            string cacheKey = $"favorite_playlist:{userId}";

            // Check if cache exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache exists - check if track already exists to avoid duplicates
                bool exists = await _redisCacheService.ListContainsAsync(cacheKey, playlistId);

                if (!exists)
                {
                    // Get current TTL to preserve it
                    var remainingTtl = await _redisCacheService.GetTTLAsync(cacheKey);
                    var ttlToSet = remainingTtl ?? TimeSpan.FromHours(1);

                    // Add track to the beginning of the list
                    await _redisCacheService.ListPushAsync(cacheKey, playlistId, ttlToSet);
                }
            }
            else
            {
                // Cache doesn't exist - create new list with this playlist
                await _redisCacheService.ListPushAsync(cacheKey, playlistId, TimeSpan.FromHours(1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add track {PlaylistId} to favorite cache for user {UserId}", playlistId, userId);
        }
    }

    private async Task RemovePlaylistFromFavoriteCacheAsync(string userId, string playlistId)
    {
        try
        {
            string cacheKey = $"favorite_playlist:{userId}";

            // Check if cache exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Get current TTL to preserve it
                var remainingTtl = await _redisCacheService.GetTTLAsync(cacheKey);

                // Remove playlist from list (removes all occurrences)
                long removedCount = await _redisCacheService.ListRemoveAsync(cacheKey, playlistId, 0);

                if (removedCount > 0)
                {
                    // Restore TTL if there are still items in the list
                    long newLength = await _redisCacheService.ListLengthAsync(cacheKey);
                    if (newLength > 0 && remainingTtl.HasValue)
                    {
                        await _redisCacheService.SetExpirationAsync(cacheKey, remainingTtl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove track {PlaylistId} from favorite cache for user {UserId}", playlistId, userId);
        }
    }
    #endregion
}
