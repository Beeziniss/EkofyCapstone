using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.REST;
[Route("api/media-streaming")]
[ApiController]
public class MediaStreamingController(IAmazonCloudFrontService amazonCloudFrontService) : ControllerBase
{
    private readonly IAmazonCloudFrontService _amazonCloudFrontService = amazonCloudFrontService;

    // FE gọi để lấy token ký cho trackId
    [HttpPost("signed-token")]
    public IActionResult GenerateSignedToken([FromBody] string trackId)
    {
        string token = _amazonCloudFrontService.GenerateHlsToken(trackId);

        return Ok(new { Message = "Get signed token successfully", token });
    }

    // FE gọi để refresh token, có thể dùng lại trackId cũ
    // Cần gửi kèm token cũ để xác thực
    // Chưa xong
    [HttpPost("refresh-signed-url")]
    public IActionResult RefreshSignedUrl([FromQuery] string trackId, [FromQuery] string oldToken)
    {
        _amazonCloudFrontService.ValidateHlsToken(trackId, oldToken);

        string newToken = _amazonCloudFrontService.RefreshSignedUrl(trackId, oldToken);

        return Ok(new { token = newToken });
    }

    // Endpoint để lấy key cho HLS, sẽ trả về file binary
    // Bên trong content của HLS.m3u8 sẽ tự động gọi đến hàm này
    [HttpGet("keys")]
    public IActionResult DecryptionKey([FromQuery] string trackId, [FromQuery] string token)
    {
        _amazonCloudFrontService.ValidateHlsToken(trackId, token);

        byte[] keyBytes = _amazonCloudFrontService.DecryptionKey(trackId, token);

        return File(keyBytes, "application/octet-stream");
    }

    // Đây là endpoint để FE gọi lấy master.m3u8, nội dung sẽ trỏ đến các bitrate.m3u8 qua API proxy của bạn
    [HttpGet("cloudfront/{trackId}/master.m3u8")]
    public async Task<IActionResult> GetMasterPlaylist(string trackId, [FromQuery] string token)
    {
        _amazonCloudFrontService.ValidateHlsToken(trackId, token);

        string signedContent = await _amazonCloudFrontService.GetMasterPlaylistAsync(trackId, token);

        return Content(signedContent, "application/vnd.apple.mpegurl");
    }

    // Hàm xử lý proxy cho file bitrate.m3u8, replace và ký toàn bộ .ts và EXT-X-KEY bên trong
    [HttpGet("{trackId}/{bitrate}/playlist.m3u8")]
    public async Task<IActionResult> GetBitratePlaylist(string trackId, string bitrate, [FromQuery] string token)
    {
        _amazonCloudFrontService.ValidateHlsToken(trackId, token);

        string finalContent = await _amazonCloudFrontService.GetBitratePlaylistAsync(trackId, bitrate, token);

        return Content(finalContent, "application/vnd.apple.mpegurl");
    }

    [HttpGet("{trackId}/{bitrate}/{segment}")]
    public IActionResult ProxySegment(string trackId, string bitrate, string segment, [FromQuery] string token)
    {
        _amazonCloudFrontService.ValidateHlsToken(trackId, token);

        string redirectUrl = _amazonCloudFrontService.GenerateSignedRedirect(trackId, bitrate, segment, token);
        return Redirect(redirectUrl);
    }
}
