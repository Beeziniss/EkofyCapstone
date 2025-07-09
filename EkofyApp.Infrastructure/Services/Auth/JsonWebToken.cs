using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EkofyApp.Infrastructure.Services.Auth;
public sealed class JsonWebToken : IJsonWebToken
{
    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {

        int expiresInDays = 30; //set default expire time is 7 days

        //get secret key from appsettings.json
        var secretKey = Environment.GetEnvironmentVariable("JWTSettings_SecretKey") ?? throw new Exception("JWT's Secret Mode property is not set in environment or not found");

        //convert secret key to byte array
        var symmetricKey = Encoding.UTF8.GetBytes(secretKey);

        //create token with JwtSecurityTokenHandler
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new JwtSecurityToken(
            //issuer: "https://localhost:7018", //set issuer is localhost

            //audience: "https://localhost:7018", //set audience is localhost

            claims: claims,

            expires: TimeControl.GetUtcPlus7Time().Add(TimeSpan.FromDays(expiresInDays)),

            signingCredentials: new SigningCredentials(
                                new SymmetricSecurityKey(symmetricKey),
                                SecurityAlgorithms.HmacSha256Signature) //use HmacSha256Signature algorithm to sign token
        );
        //write token with tokenDescriptor above
        var token = tokenHandler.WriteToken(tokenDescriptor);
        return token;
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHadler = new JwtSecurityTokenHandler();
        string key = Environment.GetEnvironmentVariable("JWTSettings_SecretKey") ?? throw new NotFoundCustomException("JWTSettings_SecretKey is not set in the environment");

        TokenValidationParameters tokenValidationParameters = new()
        {
            ValidateAudience = false,

            ValidateIssuer = false,

            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key) ?? throw new Exception("JWT's Secret Mode property is not set in environment or not found")),

            ValidateLifetime = false
        };

        //var tokenReader = tokenHadler.ReadJwtToken(token);
        //nếu secret key hợp lệ thì trả về claims chứa thông tin đã encode trong lúc tạo accesstoken
        return tokenHadler.ValidateToken(token, tokenValidationParameters, out _);
    }

    public JwtSecurityToken DecodeToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        // Giải mã token JWT mà không cần xác thực
        var decodedToken = tokenHandler.ReadJwtToken(token);
        return decodedToken;
    }
}
