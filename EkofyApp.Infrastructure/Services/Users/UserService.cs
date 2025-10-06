using BCrypt.Net;
using EkofyApp.Application.Models.Users;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
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

        await _unitOfWork.GetCollection<User>().InsertOneAsync(new User
        {
            Email = createModeratorRequest.Email.ToLowerInvariant(),
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

        await _unitOfWork.GetCollection<User>().InsertOneAsync(new User
        {
            Email = createAdminRequest.Email.ToLowerInvariant(),
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

    public async Task DeActiveUserAsync(string targetUserId)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            string currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            UpdateDefinition <User> update = Builders<User>.Update
            .Set(u => u.Status, UserStatus.Inactive)
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
