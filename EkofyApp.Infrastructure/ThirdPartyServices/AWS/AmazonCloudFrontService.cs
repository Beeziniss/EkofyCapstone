using Amazon.CloudFront;
using Amazon.S3;
using Amazon.S3.Model;
using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Settings.AWS;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace EkofyApp.Infrastructure.ThirdPartyServices.AWS;
public class AmazonCloudFrontService(IAmazonS3 s3Client, AWSSetting aWSSettings) : IAmazonCloudFrontService
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly AWSSetting _aWSSettings = aWSSettings;

    public byte[] DecryptionKey(string trackId, string token)
    {
        if(!IsAuthorized(trackId, token))
        {
            throw new UnauthorizedCustomException("Unauthorized access");
        }

        string? keyHex = Environment.GetEnvironmentVariable("HLS_KEY");

        if (string.IsNullOrWhiteSpace(keyHex) || keyHex.Length != 32)
        {
            throw new NotFoundCustomException("Encryption key is not properly configured");
        }

        return Convert.FromHexString(keyHex);
    }

    public async Task<string> GetMasterPlaylistAsync(string trackId, string token)
    {
        if (!IsAuthorized(trackId, token))
        {
            throw new UnauthorizedCustomException("Unauthorized access");
        }

        // Nhớ chỉnh lại Root Folder là Streaming Audio thay vì Testing
        string masterFilePath = $"Testing/{trackId}/{trackId}_master.m3u8";

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
                    string apiUrl = $"https://localhost:8888/api/media-streaming/{trackId}/{bitrate}/playlist.m3u8?token={token}"; // Nhớ sửa URL lại cho chuẩn
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
        if (!IsAuthorized(trackId, token))
        {
            throw new UnauthorizedCustomException("Unauthorized access");
        }

        // Nhớ chỉnh lại Root Folder là Streaming Audio thay vì Testing
        string bitrateHlsFilePath = $"Testing/{trackId}/{bitrate}/{trackId}_hls.m3u8";

        try
        {
            GetObjectResponse response = await _s3Client.GetObjectAsync(_aWSSettings.BucketName, bitrateHlsFilePath);

            using StreamReader reader = new(response.ResponseStream);
            string content = await reader.ReadToEndAsync();

            string keyUrlHidden = Environment.GetEnvironmentVariable("HLS_KEY_URL_HIDDEN") ?? throw new NotFoundCustomException("HLS_KEY_URL_HIDDEN is not configured");
            //string privateKeyPath = PathHelper.ResolvePath(PathTag.PrivateKeys);
            //privateKeyPath = Path.GetFullPath(privateKeyPath);
            string privateKeyPath = "Z:\\Projects\\EkofyProject\\EkofyCapstone\\PrivateKeys\\private_key.pem";
            string privateKey = File.ReadAllText(privateKeyPath);
            string templateHlsKeyUrl = Environment.GetEnvironmentVariable("HLS_KEY_URL") ?? throw new NotFoundCustomException("HLS_KEY_URL is not configured");
            DateTime expires = DateTime.UtcNow.AddMinutes(2);

            string finalUri = templateHlsKeyUrl
                .Replace("{trackId}", trackId)
                .Replace("{token}", token);

            List<string> signedLines = [];
            string updatedLine;

            foreach (string line in content.Split('\n'))
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("#EXT-X-KEY"))
                {
                    if (trimmed.Contains(keyUrlHidden))
                    {
                        updatedLine = trimmed.Replace(keyUrlHidden, finalUri);
                    }
                    else
                    {
                        updatedLine = Regex.Replace(trimmed, @"URI=""[^""]+""", $@"URI=""{finalUri}""");
                    }

                    signedLines.Add(updatedLine);
                    continue;
                }

                if (trimmed.EndsWith(".ts"))
                {
                    string relativePath = $"Testing/{trackId}/{bitrate}/{trimmed}";

                    string fullUrl = $"{_aWSSettings.CloudFrontDomainUrl}/{relativePath}";

                    string signedUrl = AmazonCloudFrontUrlSigner.GetCannedSignedURL(
                        fullUrl,
                        new StringReader(privateKey),
                        _aWSSettings.KeyPairId,
                        expires
                    );

                    signedLines.Add(signedUrl);
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
            throw new NotFoundCustomException($"Bitrate file not found in S3: {bitrateHlsFilePath}");
        }
    }

    private static bool IsAuthorized(string expectedTrackId, string token)
    {
        string? key = Environment.GetEnvironmentVariable("HLS_KEY");

        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

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
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
            };

            tokenHandler.ValidateToken(token, parameters, out SecurityToken? validatedToken);

            JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;
            string? tokenTrackId = jwtToken.Claims.FirstOrDefault(c => c.Type == "trackId")?.Value;

            return tokenTrackId == expectedTrackId;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateHlsToken(string trackId, int expireMinutes = 5)
    {
        if (string.IsNullOrEmpty(trackId))
        {
            throw new ValidationCustomException("Track id cannot be null or empty");
        }

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
                new Claim("trackId", trackId)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

        // Trả về token đã ký
        return tokenHandler.WriteToken(token);
    }
}
