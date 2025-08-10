using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Listeners;
using EkofyApp.Application.Models.Projections;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Auth;
public sealed class AuthenticationService(IUnitOfWork unitOfWork, IJsonWebToken jsonWebToken, IHttpContextAccessor httpContextAccessor) : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IJsonWebToken _jsonWebToken = jsonWebToken;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    private async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _unitOfWork.GetCollection<User>()
            .Find(u => u.Email == email)
            .Project(u => u.Email)
            .AnyAsync();
    }

    public async Task RegisterListenerAsync(ListenerRegisterRequest registerRequest)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            // Kiểm tra xem email tồn tại
            if (await IsEmailExistsAsync(registerRequest.Email.Trim().ToLowerInvariant()))
            {
                throw new ConflictCustomException("Email already exists.");
            }

            string userId = ObjectId.GenerateNewId().ToString();

            // Tạo người dùng mới
            User user = new()
            {
                Id = userId,
                Email = registerRequest.Email.Trim().ToLowerInvariant(),
                PasswordHash = HashPassword(registerRequest.Password),
                BirthDate = registerRequest.BirthDate.Date,
                Gender = registerRequest.Gender,
                Roles = [UserRole.Listener], // Mặc định là Listener
                IsLinkedWithGoogle = false,
            };

            Listener listener = new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserId = userId,
                Name = registerRequest.Name,
                Email = registerRequest.Email.Trim().ToLowerInvariant(),
            };

            // Lưu người dùng và listener vào cơ sở dữ liệu
            await _unitOfWork.GetCollection<User>().InsertOneAsync(user);
            await _unitOfWork.GetCollection<Listener>().InsertOneAsync(listener);
        });
    }

    public async Task<AuthListenerTokenResponse> LoginListenerAsync(LoginRequest loginRequest)
    {
        // Define the filter to find the listener by email
        FilterDefinitionBuilder<User> filterBuilder = Builders<User>.Filter;
        FilterDefinition<User> userFilter = Builders<User>.Filter.And(
            filterBuilder.Eq(l => l.Email, loginRequest.Email.Trim().ToLowerInvariant()),
            filterBuilder.Eq(l => l.Status, UserStatus.Active),
            filterBuilder.Eq(l => l.IsLinkedWithGoogle, false),
            filterBuilder.AnyEq(l => l.Roles, UserRole.Listener)
        );
        ProjectionDefinition<UserProjection> listenerUserProjection = Builders<UserProjection>.Projection
            .Include(lp => lp.Id)
            .Include(lp => lp.ListenerProjection.Id)
            .Include(lp => lp.Roles)
            .Include(lp => lp.PasswordHash);

        UserProjection userListener = await _unitOfWork.GetCollection<User>().Aggregate()
            .Match(userFilter)
            .Lookup<User, Listener, UserProjection>(
                _unitOfWork.GetCollection<Listener>(),
                user => user.Id,
                listener => listener.UserId,
                userProjection => userProjection.ListenerProjection
            )
            .Unwind(u => u.ListenerProjection, new AggregateUnwindOptions<UserProjection>
            {
                PreserveNullAndEmptyArrays = true
            })
            .Project<UserProjection>(listenerUserProjection)
            .FirstOrDefaultAsync() ?? throw new BadRequestCustomException("Invalid email or password.");

        // Kiểm tra mật khẩu
        if (!VerifyPassword(loginRequest.Password, userListener.PasswordHash!))
        {
            throw new BadRequestCustomException("Invalid email or password.");
        }

        // Tạo claims
        IEnumerable<Claim> claims =
        [
            new Claim("userId", userListener.Id),
            new Claim("listenerId",userListener.ListenerProjection.Id),
            new Claim(ClaimTypes.Role, string.Join(",", userListener.Roles)),
        ];

        // Tạo access token
        string accessToken = _jsonWebToken.GenerateAccessToken(claims);

        return new AuthListenerTokenResponse()
        {
            AccessToken = accessToken,
            UserId = userListener.Id,
            ListenerId = userListener.ListenerProjection.Id,
            Roles = userListener.Roles,
        };
    }

    // Methods
}
