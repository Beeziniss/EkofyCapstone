using EkofyApp.Application.Models.UserFollows;
using EkofyApp.Application.Models.Users;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Users;
public sealed class UserService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<User> GetUsers()
    {
        return _unitOfWork.GetCollection<User>().AsQueryable();
    }

    public async Task<User> GetUserByIdAsync(string id)
    {
        ProjectionDefinition<User> projection = Builders<User>.Projection
            .Exclude(x => x.FCMToken)
            .Exclude(x => x.PasswordHash)
            .Exclude(x => x.RefreshToken)
            .Exclude(x => x.RefreshTokenExpiryTime);

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
            FullName  = $"{UserRole.Moderator.ToString()}-{moderatorId}",
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
        if(await IsEmailExistsAsync(createAdminRequest.Email))
        {
            throw new ConflictCustomException("Email already exists.");
        }

        string adminId = ObjectId.GenerateNewId().ToString();
        await _unitOfWork.GetCollection<User>().InsertOneAsync(new User
        {
            Id = adminId,
            Email = createAdminRequest.Email.ToLowerInvariant(),
            FullName  = $"{UserRole.Admin.ToString()}-{adminId}",
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

    public IQueryable<Follows> GetUserFollows()
    {
        return _unitOfWork.GetCollection<Follows>().AsQueryable();
    }

    public async Task FollowUserAsync(FollowUserRequest request)
    {
        string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        if (currentUserId == request.TargetUserId)
        {
            throw new BadRequestCustomException("You cannot follow yourself");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Check if target user exists
            User? targetUser = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == request.TargetUserId)
                .Project<User>(Builders<User>.Projection.Include(x => x.Role))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found target user {currentUserId}");

            // Get current user info
            User? currentUser = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == currentUserId)
                .Project<User>(Builders<User>.Projection.Include(x => x.Role))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found current user {currentUserId}");

            // Check if already following
            bool existingFollow = await _unitOfWork.GetCollection<Follows>()
                .Find(f => f.FollowerId == currentUserId && f.FollowedId == request.TargetUserId)
                .AnyAsync() ? throw new ConflictCustomException("Already following this user") : false ; // Cách viết này (micro-optimization) có thật sự hiệu quả so với truyền thống?

            // Create follow relationship
            Follows follow = new()
            {
                FollowerId = currentUserId,
                FollowerType = currentUser.Role,
                FollowedId = request.TargetUserId,
                FollowedType = targetUser.Role,
                CreatedAt = HelperMethod.GetUtcPlus7TimeOffset()
            };

            await _unitOfWork.GetCollection<Follows>().InsertOneAsync(session, follow);

            // Update follower counts based on user types
            switch (targetUser.Role)
            {
                case UserRole.Artist:
                    {
                        // Update artist's follower count
                        await _unitOfWork.GetCollection<Artist>()
                            .UpdateOneAsync(session,
                                a => a.UserId == request.TargetUserId,
                                Builders<Artist>.Update
                                    .Inc(a => a.FollowerCount, 1));
                        break;
                    }

                case UserRole.Listener:
                    {
                        // Update listener's follower count
                        await _unitOfWork.GetCollection<Listener>()
                            .UpdateOneAsync(session,
                                l => l.UserId == request.TargetUserId,
                                Builders<Listener>.Update
                                    .Inc(l => l.FollowerCount, 1)
                                    .Push(l => l.LastFollowers, currentUserId));
                        break;
                    }
            }
        });
    }

    public async Task UnfollowUserAsync(UnfollowUserRequest request)
    {
        string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedCustomException("Your session is limit");

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Find the follow relationship
            Follows? follow = await _unitOfWork.GetCollection<Follows>()
                .Find(f => f.FollowerId == currentUserId && f.FollowedId == request.TargetUserId)
                .Project<Follows>(Builders<Follows>.Projection.Include(f => f.Id))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Follow relationship not found");

            // Delete the follow relationship
            await _unitOfWork.GetCollection<Follows>()
                .DeleteOneAsync(session, f => f.Id == follow.Id);

            // Get user info for updating counts
            bool currentUserExisted = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == currentUserId)
                .AnyAsync() ? true : throw new NotFoundCustomException($"Not found current user {currentUserId}");

            User? targetUser = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == request.TargetUserId)
                .Project<User>(Builders<User>.Projection.Include(x => x.Role))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found target user {currentUserId}");

            // Update follower counts based on user types
            switch (targetUser.Role)
            {
                case UserRole.Artist:
                    {
                        // Update artist's follower count
                        await _unitOfWork.GetCollection<Artist>()
                            .UpdateOneAsync(session,
                                a => a.UserId == request.TargetUserId,
                                Builders<Artist>.Update
                                    .Inc(a => a.FollowerCount, -1));
                        break;
                    }

                case UserRole.Listener:
                    {
                        // Update listener's follower count
                        await _unitOfWork.GetCollection<Listener>()
                            .UpdateOneAsync(session,
                                l => l.UserId == request.TargetUserId,
                                Builders<Listener>.Update
                                    .Inc(l => l.FollowerCount, -1)
                                    .Pull(l => l.LastFollowers, currentUserId));
                        break;
                    }
            }
        });
    }

    public async Task ReActiveUserAsync(string targetUserId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            UpdateDefinition<User> update = Builders<User>.Update
            .Set(u => u.Status, UserStatus.Active)
            .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

            UpdateResult result = await _unitOfWork.GetCollection<User>()
                .UpdateOneAsync(session, u => u.Id == targetUserId && u.Role != UserRole.Admin && u.Id != currentUserId, update);
            if (result.MatchedCount == 0)
            {
                throw new NotFoundCustomException("User not found or you cannot reactive yourself.");
            }
            if (result.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("Failed to reactivate user.");
            }
        });
    }

    public async Task BanUserAsync(string targetUserId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            UpdateDefinition <User> update = Builders<User>.Update
            .Set(u => u.Status, UserStatus.Banned)
            .Set(u => u.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());

            UpdateResult result = await _unitOfWork.GetCollection<User>()
                .UpdateOneAsync(session, u => u.Id == targetUserId && u.Role != UserRole.Admin && u.Id != currentUserId, update);
            if (result.MatchedCount == 0)
            {
                throw new NotFoundCustomException("User not found or you cannot deactive yourself.");
            }
            if (result.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("Failed to deactivate user.");
            }
        });
    }
}
