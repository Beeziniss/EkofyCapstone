using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;

namespace EkofyApp.Api.REST;
[Route("api/encryptions")]
[ApiController]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult GetKey([FromQuery] string trackId, [FromQuery] string token)
    {
        if (!IsAuthorized(token, trackId))
        {
            return Unauthorized("Invalid token or trackId");
        }

        string? keyHex = Environment.GetEnvironmentVariable("HLS_KEY");

        if (string.IsNullOrWhiteSpace(keyHex) || keyHex.Length != 32)
        {
            return StatusCode(500, "Encryption key is not properly configured");
        }

        byte[] keyBytes = Convert.FromHexString(keyHex);

        return File(keyBytes, "application/octet-stream");
    }

    private static bool IsAuthorized(string token, string trackId)
    {
        // Thay thế logic kiểm tra thật sự bằng JWT / session v.v.
        return token == "xyz" && !string.IsNullOrWhiteSpace(trackId);
    }

    //[HttpGet("{trackId}/{bitrate}/master.m3u8")]
    //public IActionResult GetM3U8(string trackId, string bitrate)
    //{
    //    //var userId = GetUserId(); // từ JWT, session, etc.
    //    //var token = JwtHelper.GenerateToken(trackId, userId, 5);

    //    string token = "xyz"; // Giả sử đã có token từ JWT hoặc session

    //    if (!IsAuthorized(token, trackId))
    //    {
    //        return Unauthorized("Invalid token or trackId");
    //    }

    //    // Load từ S3 (bằng HttpClient hoặc AWS SDK)
    //    //var s3Url = $"https://your-bucket.s3.amazonaws.com/hls/{trackId}/index.m3u8";
    //    //var raw = await _httpClient.GetStringAsync(s3Url);

    //    string keyUriTemplate = Environment.GetEnvironmentVariable("HLS_KEY_URL")!;

    //    // Replace trackId và token trong keyUriTemplate
    //    string keyUri = keyUriTemplate
    //        .Replace("{trackId}", trackId)
    //        .Replace("{token}", token);

    //    // Lấy từ local file
    //    string raw = System.IO.File.ReadAllText($"Z:\\Projects\\EkofyProject\\EkofyCapstone\\EkofyApp.Api\\bin\\Debug\\net8.0\\audio_processing\\output_hls_audio\\{trackId}\\{bitrate}kbps\\{trackId}_hls.m3u8");

    //    string finalM3u8 = raw.Replace("{KEY_URI}", keyUri);

    //    return Content(finalM3u8, "application/vnd.apple.mpegurl");
    //}

}
