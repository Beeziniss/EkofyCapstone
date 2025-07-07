using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EkofyApp.Application.ServiceInterfaces.Authentication;
public interface IJsonWebToken
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    ClaimsPrincipal ValidateToken(string token);
    JwtSecurityToken DecodeToken(string token);
}
