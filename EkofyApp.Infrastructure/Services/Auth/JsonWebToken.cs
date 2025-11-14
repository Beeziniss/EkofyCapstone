using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EkofyApp.Infrastructure.Services.Auth;
public sealed class JsonWebToken : IJsonWebToken
{
    private readonly IRedisCacheService _redisCacheService;
    public JsonWebToken(IRedisCacheService redisCacheService)
    {
        _redisCacheService = redisCacheService;
    }

    public async Task<AccessTokenResponse> GenerateAccessTokenAsync(IEnumerable<Claim> claims, bool isMobile = false)
    {
        //generate access token and refresh token (overload method)
        string accessToken = GenerateAccessToken(claims);

        string refreshToken = GenerateRefreshToken(claims, null);

        string userId = claims.FirstOrDefault(c => c.Type == "userId")!.Value.ToString();

        //lưu vào redis 7 ngày
        if (isMobile)
        {
            await _redisCacheService.SetStringAsync("jwt_mobile:" + userId, refreshToken, TimeSpan.FromDays(7));
        }
        else
        {
            await _redisCacheService.SetStringAsync("jwt:" + userId, refreshToken, TimeSpan.FromDays(7));
        }

        return new AccessTokenResponse
        {
            AccessToken = accessToken,
            ExpiresIn = (long)TimeSpan.FromDays(1).TotalSeconds,
            RefreshToken = refreshToken
        };
    }

    public async Task<AccessTokenResponse> GenerateRefreshTokenAsync(string oldRefreshToken)
    {
        string refreshSecretKey = Environment.GetEnvironmentVariable("JWTSettings_RefreshTokenSecretKey") ?? throw new NotFoundCustomException("JWT refresh secret key is not set in environment or not found");


        ClaimsPrincipal principal = GetPrincipalFromExpiredToken(oldRefreshToken);

        string userID = principal.FindFirst(c => c.Type == "userId")?.Value!;

        string? tokenInRedis = await _redisCacheService.GetStringAsync("jwt:" + userID);

        if (tokenInRedis is null || oldRefreshToken != tokenInRedis)
        {
            throw new BadRequestCustomException("Please log in again!");
        }

        string newAccessToken = GenerateAccessToken(principal.Claims);
        string newRefreshToken = GenerateRefreshToken(principal.Claims, principal);

        //set lại refresh token và thời gian hết hạn mới
        await _redisCacheService.SetStringAsync("jwt:" + userID, newRefreshToken, TimeSpan.FromDays(7));
        return new AccessTokenResponse
        {
            AccessToken = newAccessToken,
            ExpiresIn = (long)TimeSpan.FromDays(1).TotalSeconds,
            RefreshToken = newRefreshToken
        };
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHadler = new JwtSecurityTokenHandler();
        string key = Environment.GetEnvironmentVariable("JWTSettings_SecretKey") ?? throw new UnconfiguredEnvironmentCustomException("JWTSettings_SecretKey is not set in the environment");

        TokenValidationParameters tokenValidationParameters = new()
        {
            ValidateAudience = false,

            ValidateIssuer = false,

            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key) ?? throw new Exception("JWT's Secret CancelMode property is not set in environment or not found")),

            ValidateLifetime = false
        };

        //var tokenReader = tokenHadler.ReadJwtToken(token);
        //nếu secret key hợp lệ thì trả về claims chứa thông tin đã encode trong lúc tạo accesstoken
        return tokenHadler.ValidateToken(token, tokenValidationParameters, out _);
    }

    public JwtSecurityToken DecodeToken(string token)
    {
        JwtSecurityTokenHandler tokenHandler = new();

        // Giải mã token JWT mà không cần xác thực
        JwtSecurityToken decodedToken = tokenHandler.ReadJwtToken(token);
        return decodedToken;
    }

    public async Task RevokeToken(string userId, bool isMobile = false)
    {
        if(isMobile)
        {
            if (await _redisCacheService.GetStringAsync("jwt_mobile:" + userId) is null)
            {
                return;
            }

            await _redisCacheService.RemoveAsync("jwt_mobile:" + userId);
            return;
        }

        if (await _redisCacheService.GetStringAsync("jwt:" + userId) is null)
        {
            return;
        }

        await _redisCacheService.RemoveAsync("jwt:" + userId);
    }

    #region Overload method

    private string GenerateAccessToken(IEnumerable<Claim> claims)
    {

        int expiresInDays = 1;

        //get secret key from appsettings.json
        string secretKey = Environment.GetEnvironmentVariable("JWTSettings_SecretKey") ?? throw new Exception("JWT's Secret CancelMode property is not set in environment or not found");

        //convert secret key to byte array
        byte[] symmetricKey = Encoding.UTF8.GetBytes(secretKey);

        //create token with JwtSecurityTokenHandler
        JwtSecurityTokenHandler tokenHandler = new();

        JwtSecurityToken tokenDescriptor = new(
            //issuer: "https://localhost:7018", //set issuer is localhost

            //audience: "https://localhost:7018", //set audience is localhost

            claims: claims,

            expires: HelperMethod.GetUtcPlus7Time().Add(TimeSpan.FromDays(expiresInDays)),

            signingCredentials: new SigningCredentials(
                                new SymmetricSecurityKey(symmetricKey),
                                SecurityAlgorithms.HmacSha256Signature)
        //use HmacSha256Signature algorithm to sign token
        );
        //write token with tokenDescriptor above
        string token = tokenHandler.WriteToken(tokenDescriptor);
        return token;
    }

    private string GenerateRefreshToken(IEnumerable<Claim>? claims, ClaimsPrincipal? principal)
    {
        //claims ??=
        //    [
        //        new Claim(ClaimTypes.Name, principal.Identity?.Name ?? throw new ArgumentException("User's ID is not found in any session")),
        //            new Claim(ClaimTypes.Role, principal.FindFirst(ClaimTypes.Role)?.Value ?? throw new ArgumentException("User's Role is not found in any session")),
        //            new Claim("UserId", principal.FindFirst("UserId")?.Value ?? throw new ArgumentException("Artist's ID is not found in any session")),
        //            new Claim(ClaimTypes.NameIdentifier, principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new ArgumentException("User's ID is not found in any session")),
        //            new Claim("Avatar", principal.FindFirst("Avatar")?.Value ?? throw new ArgumentException("User's Avatar is not found in any session")),
        //        ];

        int expireDays = 7; //set default expire time is 7 days

        string? refreshSecretKey = Environment.GetEnvironmentVariable("JWTSettings_RefreshTokenSecretKey") ?? throw new NotFoundCustomException("JWT's Secret refresh token is not set in environment or not found");

        var symmetricKey = Encoding.UTF8.GetBytes(refreshSecretKey);

        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new JwtSecurityToken(

            claims: claims,

            expires: DateTime.Now.Add(TimeSpan.FromMinutes(expireDays)),

            signingCredentials: new SigningCredentials(
                                new SymmetricSecurityKey(symmetricKey),
                                SecurityAlgorithms.HmacSha256Signature) //use HmacSha256Signature algorithm to sign token
        );

        var token = tokenHandler.WriteToken(tokenDescriptor);
        return token;
    }

    #endregion

    #region Private methods

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string? token)
    {
        //set token validation parameters
        TokenValidationParameters tokenValidationParameters = new()
        {
            ValidateAudience = false,

            ValidateIssuer = false,

            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWTSettings_RefreshTokenSecretKey") ?? throw new NotFoundCustomException("JWT's Secret Mode property is not set in environment or not found"))), //Sign with encoded secret key

            ValidateLifetime = false //this field not need to check validate because we just want to get principal from that token
        };

        //get principal from token from tokenValidationParameters (information Claim in here)
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        //check if token is null or not and compare algorithm
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
        {
            //throw exception if information in token is invalid
            throw new SecurityTokenException("Invalid token");
        }
        return principal;
    }

    #endregion
}
