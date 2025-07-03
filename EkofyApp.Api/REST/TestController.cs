using EkofyApp.Application.ServiceInterfaces.Tracks;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.REST;
[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    [HttpPost("analyze-grpc")]
    public async Task<IActionResult> AnalyzeAudioGrpc(IFormFile file, [FromServices] IAudioAnalysisService audioAnalysisService)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var reply = await audioAnalysisService.AnalyzeAudioAsync(file.OpenReadStream());

        return Ok(reply);
    }
}
