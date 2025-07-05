using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.REST;
[Route("api/media-streaming")]
[ApiController]
public class MediaStreamingController(IAmazonCloudFrontService amazonCloudFrontService) : ControllerBase
{
    private readonly IAmazonCloudFrontService _amazonCloudFrontService = amazonCloudFrontService;

    [HttpPost("authorize")]
    public IActionResult SetSignedCookies([FromBody] string trackId)
    {
        string cloudFrontUrl = Environment.GetEnvironmentVariable("AWS_CLOUDFRONT_DOMAIN_URL")!; // e.g., https://dxxxxx.cloudfront.net
        string endpoint = Environment.GetEnvironmentVariable("ENDPOINTS_ENCRYPTION")!; // e.g., /streaming-audio

        string resourcePath = $"{cloudFrontUrl}/{endpoint}/{trackId}/*"; // Áp dụng cho mọi .ts, .m3u8, .key v.v.

        DateTime expiresAt = DateTime.UtcNow.AddMinutes(10); // Hết hạn sau 10 phút
        var cookies = _amazonCloudFrontService.GenerateSignedCookies(resourcePath, expiresAt);

        foreach (var cookie in cookies)
        {
            Response.Cookies.Append(cookie.Key, cookie.Value, new CookieOptions
            {
                Domain = new Uri(cloudFrontUrl).Host,
                HttpOnly = false,
                Secure = true,
                Path = "/",
                Expires = expiresAt
            });
        }

        return Ok(new { Message = "Signed cookies issued", ExpiresAt = expiresAt });
    }


    // FE gọi để lấy token ký cho trackId
    [HttpPost("signed-token")]
    public IActionResult GenerateSignedToken([FromBody] string trackId)
    {
        string token = _amazonCloudFrontService.GenerateHlsToken(trackId);

        return Ok(new { Message = "Get signed token successfully", token });
    }

    // Endpoint để lấy key cho HLS, sẽ trả về file binary
    // Bên trong content của HLS.m3u8 sẽ tự động gọi đến hàm này
    [HttpGet("keys")]
    public IActionResult DecryptionKey([FromQuery] string trackId, [FromQuery] string token)
    {
        byte[] keyBytes = _amazonCloudFrontService.DecryptionKey(trackId, token);

        return File(keyBytes, "application/octet-stream");
    }

    // Đây là endpoint để FE gọi lấy master.m3u8, nội dung sẽ trỏ đến các bitrate.m3u8 qua API proxy của bạn
    [HttpGet("cloudfront/{trackId}/master.m3u8")]
    public async Task<IActionResult> GetMasterPlaylist(string trackId, [FromQuery] string token)
    {
        string signedContent = await _amazonCloudFrontService.GetMasterPlaylistAsync(trackId, token);

        return Content(signedContent, "application/vnd.apple.mpegurl");
    }

    // Hàm xử lý proxy cho file bitrate.m3u8, replace và ký toàn bộ .ts và EXT-X-KEY bên trong
    [HttpGet("{trackId}/{bitrate}/playlist.m3u8")]
    public async Task<IActionResult> GetBitratePlaylist(string trackId, string bitrate, [FromQuery] string token)
    {
        string finalContent = await _amazonCloudFrontService.GetBitratePlaylistAsync(trackId, bitrate, token);

        return Content(finalContent, "application/vnd.apple.mpegurl");
    }
}
