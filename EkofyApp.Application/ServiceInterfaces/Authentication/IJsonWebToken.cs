using Microsoft.AspNetCore.Authentication.BearerToken;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EkofyApp.Application.ServiceInterfaces.Authentication;
public interface IJsonWebToken
{
    Task<AccessTokenResponse> GenerateAccessTokenAsync(IEnumerable<Claim> claims);
    Task<AccessTokenResponse> GenerateRefreshTokenAsync(string oldRefreshToken);
    ClaimsPrincipal ValidateToken(string token);
    JwtSecurityToken DecodeToken(string token);
    Task RevokeToken(string userId);

}
