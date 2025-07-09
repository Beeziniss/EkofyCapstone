using EkofyApp.Application.ServiceInterfaces.Authentication;
using HealthyNutritionApp.Application.Interfaces;
using Microsoft.AspNetCore.Http;

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

    public Task<string> LoginAsync(string email, string password)
    {
        throw new NotImplementedException();
    }

    // Methods
}
