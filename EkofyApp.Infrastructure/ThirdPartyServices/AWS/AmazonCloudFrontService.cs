using Amazon.CloudFront;
using Amazon.S3;
using Amazon.S3.Model;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Settings.AWS;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace EkofyApp.Infrastructure.ThirdPartyServices.AWS;
public sealed class AmazonCloudFrontService(IAmazonS3 s3Client, AWSSetting aWSSettings, IHttpContextAccessor httpContextAccessor) : IAmazonCloudFrontService
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly AWSSetting _aWSSettings = aWSSettings;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public void ValidateHlsToken(string expectedTrackId, string token)
    {
        //string cacheKey = $"auth:{tokenString}";
        //string? cached = await _distributedCache.GetStringAsync(cacheKey);

        //if (cached == "1") return true;
        //if (cached == "0") return false;

        string expectedUserId = _httpContextAccessor.HttpContext?.User.FindFirstValue("Id")
            ?? throw new UnauthorizedCustomException("Your session is limit");

        string key = Environment.GetEnvironmentVariable("HLS_KEY") ?? throw new NotFoundCustomException("HLS_KEY is not set in the environment");

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        JwtSecurityTokenHandler tokenHandler = new();

        try
        {
            TokenValidationParameters parameters = new()
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true, // kiểm tra hết hạn
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.FromSeconds(5) // Cho phép trễ nhẹ
            };

            tokenHandler.ValidateToken(token, parameters, out SecurityToken? validatedToken);

            JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;
            string? tokenTrackId = jwtToken.Claims.FirstOrDefault(c => c.Type == "trackId")?.Value;
            string? tokenUserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "expectedUserId")?.Value;

            bool isValid = tokenTrackId == expectedTrackId && tokenUserId == expectedUserId;

            //TimeSpan expiresIn = jwtToken.ValidTo - TimeControl.GetUtcPlus7Time() - TimeSpan.FromSeconds(2);
            //if (expiresIn < TimeSpan.FromSeconds(1))
            //    expiresIn = TimeSpan.FromSeconds(1);

            //await _distributedCache.SetStringAsync(
            //    cacheKey,
            //    isValid ? "1" : "0",
            //    new DistributedCacheEntryOptions
            //    {
            //        AbsoluteExpirationRelativeToNow = expiresIn
            //    });

            if (!isValid)
            {
                throw new UnauthorizedCustomException("Invalid token for the requested track");
            }
        }
        catch
        {
            //await _distributedCache.SetStringAsync(
            //cacheKey, "0",
            //new DistributedCacheEntryOptions
            //{
            //    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            //});

            throw new UnauthorizedCustomException("Invalid or expired token");
        }
    }

    public string GenerateHlsToken(string trackId, int expireMinutes = 5)
    {
        if (string.IsNullOrEmpty(trackId))
        {
            throw new ValidationCustomException("Track id cannot be null or empty");
        }

        string expectedUserId = _httpContextAccessor.HttpContext?.User.FindFirstValue("Id")
            ?? throw new UnauthorizedCustomException("Your session is limit");

        string? hlsKey = Environment.GetEnvironmentVariable("HLS_KEY");

        if (string.IsNullOrEmpty(hlsKey))
        {
            throw new Exception("HLS_SECRET environment variable not set");
        }

        byte[] keyBytes = Encoding.UTF8.GetBytes(hlsKey);

        JwtSecurityTokenHandler tokenHandler = new();
        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("trackId", trackId),
                new Claim("expectedUserId", expectedUserId)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        string tokenString = tokenHandler.WriteToken(token);

        // Trích thời gian hết hạn của token
        //if (token is JwtSecurityToken jwtToken)
        //{
        //    DateTimeOffset exp = jwtToken.ValidTo; // UTC
        //    TimeSpan cacheDuration = exp - TimeControl.GetUtcPlus7Time() - TimeSpan.FromSeconds(2);

        //    if (cacheDuration > TimeSpan.Zero)
        //    {
        //        await _distributedCache.SetStringAsync(
        //            $"auth:{tokenString}",
        //            "1",
        //            new DistributedCacheEntryOptions
        //            {
        //                AbsoluteExpirationRelativeToNow = cacheDuration
        //            });
        //    }
        //}

        // Trả về token đã ký
        return tokenString;
    }

    public string RefreshSignedUrl(string trackId, string oldToken, int expireMinutes = 5)
    {
        return GenerateHlsToken(trackId, expireMinutes);
    }

    public byte[] DecryptionKey(string trackId, string token)
    {
        string? keyHex = Environment.GetEnvironmentVariable("HLS_KEY");

        if (string.IsNullOrWhiteSpace(keyHex) || keyHex.Length != 32)
        {
            throw new NotFoundCustomException("Encryption key is not properly configured");
        }

        return Convert.FromHexString(keyHex);
    }

    public async Task<string> GetMasterPlaylistAsync(string trackId, string token)
    {
        // Nhớ thay thành production URL
        string localHostUrl = Environment.GetEnvironmentVariable("LOCALHOST_URL_HTTPS") ?? throw new NotFoundCustomException("LOCAL_HOST_URL is not configured");

        string prefixKey = Environment.GetEnvironmentVariable("AWS_MASTER_PREFIX_KEY") ?? throw new NotFoundCustomException("HLS_KEY_URL_HIDDEN is not configured");

        // Nhớ chỉnh lại Root Folder là Streaming Audio thay vì Testing
        string masterFilePath = $"{prefixKey}/{trackId}/{trackId}_master.m3u8";

        try
        {
            GetObjectResponse response = await _s3Client.GetObjectAsync(_aWSSettings.BucketName, masterFilePath);

            using StreamReader reader = new(response.ResponseStream);
            string content = await reader.ReadToEndAsync();

            string[] lines = content.Split('\n');
            List<string> signedLines = [];

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                // Bỏ qua dòng trống hoặc comment
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    signedLines.Add(trimmed);
                    continue;
                }

                // Nếu dòng chứa bitrate và kết thúc bằng .m3u8, chuyển hướng thành URL gọi tới API proxy
                if (trimmed.Contains("kbps") && trimmed.EndsWith(".m3u8"))
                {
                    // Chuyển hướng thành URL gọi tới API proxy .m3u8 bitrate
                    string bitrate = trimmed.Split('/')[0];
                    string apiUrl = $"{localHostUrl}/api/media-streaming/{trackId}/{bitrate}/playlist.m3u8?token={token}";
                    signedLines.Add(apiUrl);
                }
                else
                {
                    signedLines.Add(trimmed);
                }
            }

            return string.Join("\n", signedLines);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundCustomException($"Master file not found in S3: {masterFilePath}");
        }
    }

    public async Task<string> GetBitratePlaylistAsync(string trackId, string bitrate, string token)
    {
        string prefixKey = Environment.GetEnvironmentVariable("AWS_MASTER_PREFIX_KEY") ?? throw new NotFoundCustomException("HLS_KEY_URL_HIDDEN is not configured");

        // Nhớ chỉnh lại Root Folder là Streaming Audio thay vì Testing
        string bitrateHlsFilePath = $"{prefixKey}/{trackId}/{bitrate}/{trackId}_hls.m3u8";

        string keyUrlHidden = Environment.GetEnvironmentVariable("HLS_KEY_URL_HIDDEN") ?? throw new NotFoundCustomException("HLS_KEY_URL_HIDDEN is not configured");

        string localHostUrl = Environment.GetEnvironmentVariable("LOCALHOST_URL_HTTPS") ?? throw new NotFoundCustomException("LOCAL_HOST_URL is not configured");

        string keyUri = $"{localHostUrl}/api/media-streaming/keys?trackId={trackId}&token={token}";

        try
        {
            GetObjectResponse response = await _s3Client.GetObjectAsync(_aWSSettings.BucketName, bitrateHlsFilePath);

            using StreamReader reader = new(response.ResponseStream);
            string content = await reader.ReadToEndAsync();

            List<string> signedLines = [];
            string updatedLine;

            foreach (string line in content.Split('\n'))
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("#EXT-X-KEY"))
                {
                    if (trimmed.Contains(keyUrlHidden))
                    {
                        updatedLine = trimmed.Replace(keyUrlHidden, keyUri);
                    }
                    else
                    {
                        updatedLine = Regex.Replace(trimmed, @"URI=""[^""]+""", $@"URI=""{keyUri}""");
                    }

                    signedLines.Add(updatedLine);
                    continue;
                }

                #region Signed URL for .ts files
                //if (trimmed.EndsWith(".ts"))
                //{
                //    string relativePath = $"{prefixKey}/{trackId}/{bitrate}/{trimmed}";

                //    string fullUrl = $"{_aWSSettings.CloudFrontDomainUrl}/{relativePath}";

                //    string signedUrl = AmazonCloudFrontUrlSigner.GetCannedSignedURL(
                //        fullUrl,
                //        new StringReader(privateKey),
                //        _aWSSettings.KeyPairId,
                //        expires
                //    );

                //    signedLines.Add(signedUrl);
                //}
                //else
                //{
                //    signedLines.Add(trimmed);
                //}
                #endregion

                #region Signed Cookies for .ts files
                //if (trimmed.EndsWith(".ts"))
                //{
                //    string relativePath = $"{prefixKey}/{trackId}/{bitrate}/{trimmed}";
                //    string fullUrl = $"{_aWSSettings.CloudFrontDomainUrl}/{relativePath}";

                //    // KHÔNG ký nữa
                //    signedLines.Add(fullUrl);

                //    continue; // chỗ này có thể dùng else
                //}

                //// Các dòng khác như #EXTINF, #EXTM3U, v.v. giữ nguyên
                //signedLines.Add(trimmed);
                #endregion

                #region API proxy
                if (trimmed.EndsWith(".ts"))
                {
                    // Chuyển hướng thành URL gọi tới API proxy .ts
                    string proxyUrl = $"{localHostUrl}/api/media-streaming/{trackId}/{bitrate}/{trimmed}?token={token}";
                    signedLines.Add(proxyUrl);
                }
                else
                {
                    signedLines.Add(trimmed);
                }
                #endregion
            }

            return string.Join("\n", signedLines);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundCustomException($"OriginalBitrate file not found in S3: {bitrateHlsFilePath}");
        }
    }

    public string GenerateSignedRedirect(string trackId, string bitrate, string segment, string token)
    {
        string prefixKey = Environment.GetEnvironmentVariable("AWS_MASTER_PREFIX_KEY") ?? throw new NotFoundCustomException("AWS_MASTER_PREFIX_KEY is not configured");

        // Đường dẫn đến file .ts
        string relativePath = $"{prefixKey}/{trackId}/{bitrate}/{segment}";

        string domainUrl = Environment.GetEnvironmentVariable("AWS_CLOUDFRONT_DOMAIN_URL") ?? throw new NotFoundCustomException("AWS_CLOUDFRONT_DOMAIN_URL is not configured");

        // Tạo URL đầy đủ
        string fullUrl = $"{domainUrl}/{relativePath}";

        // Đọc private key từ file
        string privateKeyPath = PathHelper.ResolvePath(PathTag.Base, "PrivateKeys");
        privateKeyPath = Path.GetFullPath(Path.Combine(privateKeyPath, "private_key.pem"));
        using StreamReader privateKeyStream = new(privateKeyPath);

        // Thời gian hết hạn của signed URL
        DateTime expires = TimeControl.GetUtcPlus7Time().AddMinutes(2);

        // Ký URL
        string signedUrl = AmazonCloudFrontUrlSigner.GetCannedSignedURL(
            fullUrl,
            privateKeyStream,
            _aWSSettings.KeyPairId,
            expires
        );

        return signedUrl;
    }
}
