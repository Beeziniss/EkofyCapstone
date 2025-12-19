using EkofyApp.Application.Models.Albums;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Albums;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Artist;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Albums;

public sealed class AlbumService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService, ILogger<AlbumService> logger) : IAlbumService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly ILogger<AlbumService> _logger = logger;

    public IQueryable<Album> GetAlbums()
    {
        return _unitOfWork.GetCollection<Album>().AsQueryable();
    }

    public IQueryable<Album> GetFavoriteAlbums()
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        List<string> favoriteAlbumIds = _unitOfWork.GetCollection<UserEngagement>()
            .Find(x => x.ActorId == userId && x.TargetType == UserEngagementTargetType.Album && x.Action == UserEngagementAction.Like)
            .Project(x => x.TargetId)
            .ToList();

        IQueryable<Album> query = _unitOfWork.GetCollection<Album>()
            .Find(x => favoriteAlbumIds.Contains(x.Id))
            .ToEnumerable()
            .AsQueryable();

        return query;
    }

    public IQueryable<Album> SearchAlbums(string name)
    {
        IQueryable<Album> query = _unitOfWork.GetCollection<Album>().AsQueryable();

        if (string.IsNullOrEmpty(name))
        {
            return query;
        }

        string unsignedSearchTerm = HelperMethod.ToUnsigned(name);
        query = query.Where(a => a.NameUnsigned.Contains(unsignedSearchTerm));

        return query;
    }

    public async Task CreateAlbumAsync(CreateAlbumRequest createAlbumRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Ensure the creating artist is included in the album's artist infos
        if (!createAlbumRequest.ArtistInfos.Any(a => a.ArtistId == artistId))
        {
            throw new BadRequestCustomException("You must be included as an artist in the album.");
        }

        await _unitOfWork.GetCollection<Album>().InsertOneAsync(new Album()
        {
            Name = createAlbumRequest.Name,
            NameUnsigned = HelperMethod.ToUnsigned(createAlbumRequest.Name),
            Description = createAlbumRequest.Description,
            Type = createAlbumRequest.Type,
            TrackIds = createAlbumRequest.TrackIds,
            ContributingArtists = createAlbumRequest.ArtistInfos,
            CoverImage = createAlbumRequest.CoverImage ?? string.Empty,
            ThumbnailImage = createAlbumRequest.ThumbnailImage,
            ReleaseInfo = createAlbumRequest.ReleaseInfo,
            IsVisible = createAlbumRequest.IsVisible,
            CreatedBy = userId,
        });
    }

    public async Task AddTrackToAlbumAsync(AddTrackToAlbumRequest addTrackToAlbumRequest)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        Album? album = await _unitOfWork.GetCollection<Album>()
            .Find(x => x.Id == addTrackToAlbumRequest.AlbumId)
            .Project<Album>(Builders<Album>.Projection
                .Include(x => x.Id)
                .Include(x => x.TrackIds)
                .Include(x => x.ContributingArtists))
            .FirstOrDefaultAsync();

        if (album == null)
        {
            // Create new album if it doesn't exist and album name is provided
            if (string.IsNullOrEmpty(addTrackToAlbumRequest.AlbumName))
            {
                throw new BadRequestCustomException("Album not found and no album name provided for creation.");
            }

            await _unitOfWork.GetCollection<Album>().InsertOneAsync(new Album()
            {
                Name = addTrackToAlbumRequest.AlbumName!,
                NameUnsigned = HelperMethod.ToUnsigned(addTrackToAlbumRequest.AlbumName!),
                Type = AlbumType.Album,
                TrackIds = [addTrackToAlbumRequest.TrackId],
                ContributingArtists = [new() { ArtistId = artistId, Role = ArtistRole.Main }],
                CoverImage = string.Empty,
                ReleaseInfo = new() { IsRelease = false, ReleaseStatus = ReleaseStatus.NotAnnounced },
                IsVisible = true,
                CreatedBy = userId,
            });

            return;
        }

        // Check if user is one of the album artists
        if (!album.ContributingArtists.Any(a => a.ArtistId == artistId))
        {
            throw new UnauthorizedCustomException("You don't have permission to add tracks to this album.");
        }

        if (album.TrackIds.Contains(addTrackToAlbumRequest.TrackId))
        {
            throw new BadRequestCustomException("Track already exists in the album.");
        }

        UpdateDefinition<Album> updateDefinition = Builders<Album>.Update
            .Push(x => x.TrackIds, addTrackToAlbumRequest.TrackId);
        
        UpdateResult updateResult = await _unitOfWork.GetCollection<Album>()
            .UpdateOneAsync(x => x.Id == album.Id, updateDefinition);
    }

    public async Task AddToFavoriteAlbumAsync(string albumId, bool isAdding)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string role = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();
        DateTimeOffset startOfDay = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        DateTimeOffset endOfDay = startOfDay.AddDays(1);

        // If isAdding is false, remove album from favorites
        if (!isAdding)
        {
            // Remove album from user's favorite cache
            await RemoveAlbumFromFavoriteCacheAsync(userId, albumId);

            // Remove album from user's favorites in UserEngagement
            await _unitOfWork.GetCollection<UserEngagement>()
                .DeleteOneAsync(x => x.ActorId == userId && x.TargetId == albumId && x.TargetType == UserEngagementTargetType.Album && x.Action == UserEngagementAction.Like);

            return;
        }

        // Add album to user's favorites in UserEngagement
        await _unitOfWork.GetCollection<UserEngagement>()
            .InsertOneAsync(new UserEngagement
            {
                ActorId = userId,
                ActorType = Enum.Parse<UserRole>(role) == UserRole.Listener ? UserEngagementTargetType.Listener : UserEngagementTargetType.Artist,
                TargetId = albumId,
                TargetType = UserEngagementTargetType.Album,
                Action = UserEngagementAction.Like,
            });

        // Add album to user's favorite cache
        await AddAlbumToFavoriteCacheAsync(userId, albumId);
    }

    public async Task RemoveTrackFromAlbumAsync(RemoveTrackFromAlbumRequest removeTrackFromAlbumRequest)
    {
        string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Check if user has permission to modify the album
        Album? album = await _unitOfWork.GetCollection<Album>()
            .Find(x => x.Id == removeTrackFromAlbumRequest.AlbumId)
            .Project<Album>(Builders<Album>.Projection
                .Include(x => x.Id)
                .Include(x => x.ContributingArtists))
            .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Album not found.");
        if (!album.ContributingArtists.Any(a => a.ArtistId == artistId))
        {
            throw new UnauthorizedCustomException("You don't have permission to remove tracks from this album.");
        }

        UpdateDefinition<Album> updateDefinition = Builders<Album>.Update
            .Pull(x => x.TrackIds, removeTrackFromAlbumRequest.TrackId);
        
        UpdateResult updateResult = await _unitOfWork.GetCollection<Album>()
            .UpdateOneAsync(x => x.Id == removeTrackFromAlbumRequest.AlbumId, updateDefinition);

        if (updateResult.ModifiedCount == 0)
        {
            throw new BadRequestCustomException("Track does not exist in the album.");
        }
    }

    public async Task DeleteAlbumAsync(string albumId)
    {
        string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("artistId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        // Check if user has permission to delete the album
        Album? album = await _unitOfWork.GetCollection<Album>()
            .Find(x => x.Id == albumId)
            .Project<Album>(Builders<Album>.Projection
                .Include(x => x.Id)
                .Include(x => x.ContributingArtists))
            .FirstOrDefaultAsync();

        if (album == null)
        {
            throw new NotFoundCustomException("Album not found.");
        }

        if (!album.ContributingArtists.Any(a => a.ArtistId == artistId))
        {
            throw new UnauthorizedCustomException("You don't have permission to delete this album.");
        }

        DeleteResult deleteResult = await _unitOfWork.GetCollection<Album>()
            .DeleteOneAsync(x => x.Id == albumId);

        if (deleteResult.DeletedCount == 0)
        {
            throw new NotFoundCustomException("Album does not exist.");
        }
    }

    #region Caching
    public async Task<bool> CheckAlbumInFavoriteAsync(string albumId)
    {
        string? userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        try
        {
            string cacheKey = $"favorite_album:{userId}";

            // Check if cache exists and has items
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache hit - check if album exists in Redis list
                return await _redisCacheService.ListContainsAsync(cacheKey, albumId);
            }

            // Cache miss - populate from database
            await EnsureCachePopulatedAsync(userId);

            // Check again after population
            return await _redisCacheService.ListContainsAsync(cacheKey, albumId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if album {AlbumId} is in favorite for user {UserId}", albumId, userId);
            return false;
        }
    }

    private async Task<bool> EnsureCachePopulatedAsync(string userId)
    {
        try
        {
            string cacheKey = $"favorite_album:{userId}";

            // Check if cache already exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);
            if (listLength > 0)
            {
                return true; // Cache already populated
            }

            // Fetch favorite albums from database
            List<string> favoriteAlbumIds = await _unitOfWork.GetCollection<UserEngagement>()
                .Find(x => x.ActorId == userId && x.TargetType == UserEngagementTargetType.Album && x.Action == UserEngagementAction.Like)
                .Project(x => x.TargetId)
                .ToListAsync();

            if (favoriteAlbumIds.Count > 0)
            {
                // Populate cache with album IDs
                await _redisCacheService.ListPushRangeAsync(cacheKey, favoriteAlbumIds, TimeSpan.FromHours(1));

                _logger.LogDebug("Populated favorite cache for user {UserId} with {Count} albums", userId, favoriteAlbumIds.Count);
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

    private async Task AddAlbumToFavoriteCacheAsync(string userId, string albumId)
    {
        string cacheKey = $"favorite_album:{userId}";

        // Add album ID to user's favorite album cache in Redis
        await _redisCacheService.ListPushAsync(cacheKey, albumId, TimeSpan.FromHours(1));
    }

    private async Task RemoveAlbumFromFavoriteCacheAsync(string userId, string albumId)
    {
        string cacheKey = $"favorite_album:{userId}";

        // Remove album ID from user's favorite album cache in Redis
        await _redisCacheService.ListRemoveAsync(cacheKey, albumId);
    }
    #endregion
}