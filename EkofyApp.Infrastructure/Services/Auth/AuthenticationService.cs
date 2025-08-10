using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

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

    public Task<string> LoginAsync(string email, string password)
    {
        throw new NotImplementedException();
    }

    // Methods
}
