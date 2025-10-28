using EkofyApp.Application.Models.UserEngagements;
using EkofyApp.Application.Models.Users;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Users;
public sealed class UserService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IRedisCacheService redisCacheService, ILogger<UserService> logger) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IRedisCacheService _redisCacheService = redisCacheService;
    private readonly ILogger<UserService> _logger = logger;

    public IQueryable<User> GetUsers()
    {
        return _unitOfWork.GetCollection<User>().AsQueryable();
    }

    public async Task<User> GetUserByIdAsync(string id)
    {
        ProjectionDefinition<User> projection = Builders<User>.Projection
            .Exclude(x => x.FCMToken)
            .Exclude(x => x.PasswordHash);

        return await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == id)
            .Project<User>(projection)
            .FirstOrDefaultAsync();
    }

    public async Task CreateModeratorAsync(CreateModeratorRequest createModeratorRequest)
    {
        if (await IsEmailExistsAsync(createModeratorRequest.Email))
        {
            throw new ConflictCustomException("Email already exists.");
        }

        string moderatorId = ObjectId.GenerateNewId().ToString();
        await _unitOfWork.GetCollection<User>().InsertOneAsync(new User
        {
            Id = moderatorId,
            Email = createModeratorRequest.Email.ToLowerInvariant(),
            FullName = createModeratorRequest.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(createModeratorRequest.Password),

            BirthDate = DateTimeOffset.MinValue, // Lý do dùng min vì không nên thay đổi cấu trúc non-nullable sang nullable chỉ vì 2 role là Moderator và Admin

            Gender = UserGender.NotSpecified,
            Role = UserRole.Moderator,
            Status = UserStatus.Active,
            IsLinkedWithGoogle = false,
        });
    }

    public async Task CreateAdminAsync(CreateAdminRequest createAdminRequest)
    {
        if (await IsEmailExistsAsync(createAdminRequest.Email))
        {
            throw new ConflictCustomException("Email already exists.");
        }

        string adminId = ObjectId.GenerateNewId().ToString();
        await _unitOfWork.GetCollection<User>().InsertOneAsync(new User
        {
            Id = adminId,
            Email = createAdminRequest.Email.ToLowerInvariant(),
            FullName = createAdminRequest.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(createAdminRequest.Password),

            BirthDate = DateTimeOffset.MinValue, // Lý do dùng min vì không nên thay đổi cấu trúc non-nullable sang nullable chỉ vì 2 role là Moderator và Admin

            Gender = UserGender.NotSpecified,
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            IsLinkedWithGoogle = false,
        });
    }

    private async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _unitOfWork.GetCollection<User>()
            .Find(u => u.Email == email.ToLowerInvariant())
            .Project(u => u.Email)
            .AnyAsync();
    }

    public IQueryable<UserEngagement> GetUserEngagement()
    {
        return _unitOfWork.GetCollection<UserEngagement>().AsQueryable();
    }

    public async Task FollowUserAsync(UserEngagementRequest request)
    {
        string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        if (currentUserId == request.TargetId)
        {
            throw new BadRequestCustomException("You cannot follow yourself");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Check if already following
            bool existingFollow = await _unitOfWork.GetCollection<UserEngagement>()
                .Find(f => f.ActorId == currentUserId && f.TargetId == request.TargetId)
                .AnyAsync() ? throw new ConflictCustomException("Already following this user") : false; // Cách viết này (micro-optimization) có thật sự hiệu quả so với truyền thống?

            // Check if target user exists
            User? targetUser = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == request.TargetId)
                .Project<User>(Builders<User>.Projection.Include(x => x.Role))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found target user {currentUserId}");

            // Get current user info
            User? currentUser = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == currentUserId)
                .Project<User>(Builders<User>.Projection.Include(x => x.Role))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found current user {currentUserId}");

            UserEngagementTargetType followerType = currentUser.Role == UserRole.Artist ? UserEngagementTargetType.Artist : UserEngagementTargetType.Listener;
            UserEngagementTargetType followedType = targetUser.Role == UserRole.Artist ? UserEngagementTargetType.Artist : UserEngagementTargetType.Listener;

            // Create follow relationship
            UserEngagement follow = new()
            {
                ActorId = currentUserId,
                ActorType = followerType,
                TargetId = request.TargetId,
                TargetType = followedType,
                CreatedAt = HelperMethod.GetUtcPlus7TimeOffset()
            };

            await _unitOfWork.GetCollection<UserEngagement>().InsertOneAsync(session, follow);

            // Update follower counts based on user types
            switch (targetUser.Role)
            {
                case UserRole.Artist:
                    {
                        // Update artist's follower count
                        UpdateResult updateArtistFollowerCount = await _unitOfWork.GetCollection<Artist>()
                            .UpdateOneAsync(session,
                                a => a.UserId == request.TargetId,
                                Builders<Artist>.Update
                                    .Inc(a => a.FollowerCount, 1));

                        if (updateArtistFollowerCount.MatchedCount == 0)
                        {
                            throw new NotFoundCustomException($"Artist profile for user {request.TargetId} not found");
                        }
                        if (updateArtistFollowerCount.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException("Failed to update artist's follower count");
                        }

                        break;
                    }

                case UserRole.Listener:
                    {
                        // Update listener's follower count
                        UpdateResult updateListenerFollowerCount = await _unitOfWork.GetCollection<Listener>()
                            .UpdateOneAsync(session,
                                l => l.UserId == request.TargetId,
                                Builders<Listener>.Update
                                    .Inc(l => l.FollowerCount, 1)
                                    .PushEach(l => l.LastFollowers, [currentUserId], position: 0, slice: 10));

                        if (updateListenerFollowerCount.MatchedCount == 0)
                        {
                            throw new NotFoundCustomException($"Listener profile for user {request.TargetId} not found");
                        }
                        if (updateListenerFollowerCount.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException("Failed to update listener's follower count");
                        }

                        // Update current user's following count
                        UpdateResult updateCurrentUserFollowingCount = await _unitOfWork.GetCollection<Listener>()
                        .UpdateOneAsync(session,
                            l => l.UserId == currentUserId,
                            Builders<Listener>.Update
                                .Inc(l => l.FollowingCount, 1)
                                .PushEach(l => l.LastFollowings, [request.TargetId], position: 0, slice: 10));

                        if (updateCurrentUserFollowingCount.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException("Failed to update current user's following count");
                        }

                        break;
                    }
            }

            await AddUserFollowingCacheAsync(currentUserId, request.TargetId);
        });
    }

    public async Task UnfollowUserAsync(UserEngagementRequest request)
    {
        string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Find the follow relationship
            UserEngagement? follow = await _unitOfWork.GetCollection<UserEngagement>()
                .Find(f => f.ActorId == currentUserId && f.TargetId == request.TargetId)
                .Project<UserEngagement>(Builders<UserEngagement>.Projection.Include(f => f.Id))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Follow relationship not found");

            // Delete the follow relationship
            await _unitOfWork.GetCollection<UserEngagement>()
                .DeleteOneAsync(session, f => f.Id == follow.Id);

            // Get user info for updating counts
            bool currentUserExisted = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == currentUserId)
                .AnyAsync() ? true : throw new NotFoundCustomException($"Not found current user {currentUserId}");

            User? targetUser = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == request.TargetId)
                .Project<User>(Builders<User>.Projection.Include(x => x.Role))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found target user {currentUserId}");

            // Update follower counts based on user types
            switch (targetUser.Role)
            {
                case UserRole.Artist:
                    {
                        // Update artist's follower count
                        UpdateResult updateArtistFollowerCount = await _unitOfWork.GetCollection<Artist>()
                            .UpdateOneAsync(session,
                                a => a.UserId == request.TargetId,
                                Builders<Artist>.Update
                                    .Inc(a => a.FollowerCount, -1));

                        if (updateArtistFollowerCount.MatchedCount == 0)
                        {
                            throw new NotFoundCustomException($"Artist profile for user {request.TargetId} not found");
                        }
                        if (updateArtistFollowerCount.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException("Failed to update artist's follower count");
                        }

                        break;
                    }

                case UserRole.Listener:
                    {
                        // Update listener's follower count
                        UpdateResult updateListenerFollowerCount = await _unitOfWork.GetCollection<Listener>()
                            .UpdateOneAsync(session,
                                l => l.UserId == request.TargetId,
                                Builders<Listener>.Update
                                    .Inc(l => l.FollowerCount, -1)
                                    .Pull(l => l.LastFollowers, currentUserId));

                        if (updateListenerFollowerCount.MatchedCount == 0)
                        {
                            throw new NotFoundCustomException($"Listener profile for user {request.TargetId} not found");
                        }
                        if (updateListenerFollowerCount.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException("Failed to update listener's follower count");
                        }

                        // Also update current user's following count
                        UpdateResult updateCurrentUserFollowingCount = await _unitOfWork.GetCollection<Listener>()
                            .UpdateOneAsync(session,
                                l => l.UserId == currentUserId,
                                Builders<Listener>.Update
                                    .Inc(l => l.FollowingCount, -1)
                                    .Pull(l => l.LastFollowings, request.TargetId));

                        if (updateCurrentUserFollowingCount.ModifiedCount == 0)
                        {
                            throw new UnprocessableEntityCustomException("Failed to update current user's following count");
                        }

                        break;
                    }
            }

            await RemoveUserFollowingCacheAsync(currentUserId, request.TargetId);
        });
    }

    public async Task UnbanUserAsync(string targetUserId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            UpdateDefinition<User> update = Builders<User>.Update
                .Set(u => u.Status, UserStatus.Active)
                .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

            FilterDefinition<User> userFilter = Builders<User>.Filter.Eq(u => u.Id, targetUserId) &
                Builders<User>.Filter.Ne(u => u.Role, UserRole.Admin) &
                Builders<User>.Filter.Ne(u => u.Id, currentUserId) &
                (Builders<User>.Filter.Eq(u => u.Status, UserStatus.Banned) | Builders<User>.Filter.Eq(u => u.Status, UserStatus.Suspended));

            User user = await _unitOfWork.GetCollection<User>()
                .FindOneAndUpdateAsync<User>(session, userFilter, update,
                    new FindOneAndUpdateOptions<User, User>
                    {
                        ReturnDocument = ReturnDocument.Before,
                        Projection = Builders<User>.Projection.Include(x => x.Role),
                    }) ?? throw new NotFoundCustomException("Not found user or user hasn't banned/suspended");

            if (user.Role == UserRole.Listener)
            {
                UpdateResult listenerProfileUpdate = await _unitOfWork.GetCollection<Listener>()
                .UpdateOneAsync(session, l => l.UserId == targetUserId,
                    Builders<Listener>.Update
                        .Set(l => l.IsVisible, true)
                        .Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (listenerProfileUpdate.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to hide listener profile.");
                }
            }
            else if (user.Role == UserRole.Artist)
            {
                UpdateResult artistProfileUpdate = await _unitOfWork.GetCollection<Artist>()
                .UpdateOneAsync(session, a => a.UserId == targetUserId,
                    Builders<Artist>.Update.Set(a => a.IsVisible, true).Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (artistProfileUpdate.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to hide artist profile.");
                }

                UpdateResult trackUpdateRestriction = await _unitOfWork.GetCollection<Track>()
                .UpdateManyAsync(session, u => u.MainArtistIds.Contains(targetUserId),
                    Builders<Track>.Update.Set(u => u.Restriction,
                        new Restriction
                        {
                            Type = RestrictionType.None,
                        }).Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (trackUpdateRestriction.MatchedCount > 0 && trackUpdateRestriction.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to restrict user's tracks.");
                }
            }

            if (user.Role != UserRole.Moderator)
            {
                UpdateResult commentUpdate = await _unitOfWork.GetCollection<Comment>()
                .UpdateManyAsync(session, c => c.CommenterId == targetUserId,
                    Builders<Comment>.Update.Set(c => c.IsVisible, true).Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (commentUpdate.MatchedCount > 0 && commentUpdate.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to hide user's comments.");
                }

                UpdateResult playlistUpdate = await _unitOfWork.GetCollection<Playlist>()
                    .UpdateManyAsync(session, p => p.UserId == targetUserId,
                        Builders<Playlist>.Update.Set(p => p.IsVisible, true).Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (playlistUpdate.MatchedCount > 0 && playlistUpdate.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to hide user's playlists.");
                }
            }
        });
    }

    public async Task BanUserAsync(string targetUserId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            UpdateDefinition<User> update = Builders<User>.Update
                .Set(u => u.Status, UserStatus.Banned)
                .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

            FilterDefinition<User> userFilter = Builders<User>.Filter.Eq(u => u.Id, targetUserId) &
                Builders<User>.Filter.Ne(u => u.Role, UserRole.Admin) &
                Builders<User>.Filter.Ne(u => u.Id, currentUserId) &
                (Builders<User>.Filter.Eq(u => u.Status, UserStatus.Active) | Builders<User>.Filter.Eq(u => u.Status, UserStatus.Suspended));

            User user = await _unitOfWork.GetCollection<User>()
                .FindOneAndUpdateAsync<User>(session, u => u.Id == targetUserId && u.Role != UserRole.Admin && u.Id != currentUserId, update,
                    new FindOneAndUpdateOptions<User, User>
                    {
                        ReturnDocument = ReturnDocument.Before,
                        Projection = Builders<User>.Projection.Include(x => x.Role),
                    });

            if (user.Role == UserRole.Listener)
            {
                UpdateResult listenerProfileUpdate = await _unitOfWork.GetCollection<Listener>()
                .UpdateOneAsync(session, l => l.UserId == targetUserId,
                    Builders<Listener>.Update
                        .Set(l => l.IsVisible, false)
                        .Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (listenerProfileUpdate.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to hide listener profile.");
                }
            }
            else if (user.Role == UserRole.Artist)
            {
                UpdateResult artistProfileUpdate = await _unitOfWork.GetCollection<Artist>()
                .UpdateOneAsync(session, a => a.UserId == targetUserId,
                    Builders<Artist>.Update.Set(a => a.IsVisible, false).Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (artistProfileUpdate.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to hide artist profile.");
                }

                UpdateResult trackUpdateRestriction = await _unitOfWork.GetCollection<Track>()
                .UpdateManyAsync(session, u => u.MainArtistIds.Contains(targetUserId),
                    Builders<Track>.Update.Set(u => u.Restriction,
                        new Restriction
                        {
                            Type = RestrictionType.Banned,
                            Reason = "User is banned",
                            RestrictedAt = HelperMethod.GetUtcPlus7TimeOffset(),
                        }).Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (trackUpdateRestriction.MatchedCount > 0 && trackUpdateRestriction.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to restrict user's tracks.");
                }
            }

            if (user.Role != UserRole.Moderator)
            {
                UpdateResult commentUpdate = await _unitOfWork.GetCollection<Comment>()
                .UpdateManyAsync(session, c => c.CommenterId == targetUserId,
                    Builders<Comment>.Update.Set(c => c.IsVisible, false).Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (commentUpdate.MatchedCount > 0 && commentUpdate.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to hide user's comments.");
                }

                UpdateResult playlistUpdate = await _unitOfWork.GetCollection<Playlist>()
                    .UpdateManyAsync(session, p => p.UserId == targetUserId,
                        Builders<Playlist>.Update.Set(p => p.IsVisible, false).Set(l => l.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));
                if (playlistUpdate.MatchedCount > 0 && playlistUpdate.ModifiedCount == 0)
                {
                    throw new UnprocessableEntityCustomException("Failed to hide user's playlists.");
                }
            }
        });
    }

    public async Task DeleteUserManualAsync(string userId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
            User user = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == userId)
                .Project<User>(Builders<User>.Projection
                    .Include(x => x.Role))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("User not found");

            DeleteResult result = await _unitOfWork.GetCollection<User>()
                .DeleteOneAsync(session, u => u.Id == userId && u.Role != UserRole.Admin && u.Id != currentUserId);
            if (result.DeletedCount == 0)
            {
                throw new NotFoundCustomException("User not found or you cannot delete yourself.");
            }

            if (user.Role == UserRole.Artist)
            {
                DeleteResult artistProfileResult = await _unitOfWork.GetCollection<Artist>()
                    .DeleteOneAsync(session, a => a.UserId == userId);
                if (artistProfileResult.DeletedCount == 0)
                {
                    throw new NotFoundCustomException("Cannot delete artist profile.");
                }
            }
            else if (user.Role == UserRole.Listener)
            {
                DeleteResult listenerProfileResult = await _unitOfWork.GetCollection<Listener>()
                    .DeleteOneAsync(session, l => l.UserId == userId);
                if (listenerProfileResult.DeletedCount == 0)
                {
                    throw new NotFoundCustomException("Cannot delete listener profile.");
                }
            }

            DeleteResult userSubscriptionResult = await _unitOfWork.GetCollection<UserSubscription>()
                .DeleteManyAsync(session, us => us.UserId == userId);
            if (userSubscriptionResult.DeletedCount == 0)
            {
                throw new NotFoundCustomException("Cannot delete user subscription.");
            }

            DeleteResult effectiveEntitlementUserResult = await _unitOfWork.GetCollection<EffectiveEntitlement>()
                .DeleteManyAsync(session, ee => ee.UserId == userId);
            if (effectiveEntitlementUserResult.DeletedCount == 0)
            {
                throw new NotFoundCustomException("Cannot delete effective entitlement.");
            }

            DeleteResult playlistResult = await _unitOfWork.GetCollection<Playlist>()
                .DeleteManyAsync(session, p => p.UserId == userId);
            //if (playlistResult.DeletedCount == 0)
            //{
            //    throw new NotFoundCustomException("Cannot delete playlist.");
            //}

            DeleteResult followResult = await _unitOfWork.GetCollection<UserEngagement>()
                .DeleteManyAsync(session, f => f.ActorId == userId || f.TargetId == userId);
            //if (followResult.DeletedCount == 0)
            //{
            //    throw new NotFoundCustomException("Cannot delete follows.");
            //}
        });
    }

    #region Caching
    public async Task<bool> CheckUserFollowingAsync(string userFollowingId)
    {
        string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
        string role = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

        try
        {
            string cacheKey = $"favorite_following:{userId}";

            // Check if cache exists and has items
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache hit - check if following exists in Redis list
                return await _redisCacheService.ListContainsAsync(cacheKey, userFollowingId);
            }

            // Cache miss - populate from database
            if(Enum.Parse<UserRole>(role, true) == UserRole.Artist)
            {
                await EnsureCachePopulatedAsync(userId, UserEngagementTargetType.Artist);
            }
            else if(Enum.Parse<UserRole>(role, true) == UserRole.Listener)
            {
                await EnsureCachePopulatedAsync(userId, UserEngagementTargetType.Listener);
            }

            // Check again after population
            return await _redisCacheService.ListContainsAsync(cacheKey, userFollowingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if track {PlaylistId} is in favorite for user {UserId}", userFollowingId, userId);
            return false;
        }
    }

    private async Task<bool> EnsureCachePopulatedAsync(string userId, UserEngagementTargetType userEngagementTargetType)
    {
        try
        {
            string cacheKey = $"favorite_following:{userId}";

            // Check if cache already exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);
            if (listLength > 0)
            {
                return true; // Cache already populated
            }

            // Fetch favorite playlist from database
            List<string> favoritePlaylistIds = await _unitOfWork.GetCollection<UserEngagement>()
                .Find(x => x.ActorId == userId && x.TargetType == userEngagementTargetType && x.Action == UserEngagementAction.Follow)
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

    private async Task AddUserFollowingCacheAsync(string userId, string userFollowingId)
    {
        try
        {
            string cacheKey = $"favorite_following:{userId}";

            // Check if cache exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Cache exists - check if following already exists to avoid duplicates
                bool exists = await _redisCacheService.ListContainsAsync(cacheKey, userFollowingId);

                if (!exists)
                {
                    // Get current TTL to preserve it
                    var remainingTtl = await _redisCacheService.GetTTLAsync(cacheKey);
                    var ttlToSet = remainingTtl ?? TimeSpan.FromHours(1);

                    // Add following to the beginning of the list
                    await _redisCacheService.ListPushAsync(cacheKey, userFollowingId, ttlToSet);
                }
            }
            else
            {
                // Cache doesn't exist - create new list with this following
                await _redisCacheService.ListPushAsync(cacheKey, userFollowingId, TimeSpan.FromHours(1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add track {FollowingId} to favorite cache for user {UserId}", userFollowingId, userId);
        }
    }

    private async Task RemoveUserFollowingCacheAsync(string userId, string userFollowingId)
    {
        try
        {
            string cacheKey = $"favorite_following:{userId}";

            // Check if cache exists
            long listLength = await _redisCacheService.ListLengthAsync(cacheKey);

            if (listLength > 0)
            {
                // Get current TTL to preserve it
                var remainingTtl = await _redisCacheService.GetTTLAsync(cacheKey);

                // Remove following from list (removes all occurrences)
                long removedCount = await _redisCacheService.ListRemoveAsync(cacheKey, userFollowingId, 0);

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
            _logger.LogError(ex, "Failed to remove track {FollowingId} from favorite cache for user {UserId}", userFollowingId, userId);
        }
    }
    #endregion
}
