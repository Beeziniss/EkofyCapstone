using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.REST;
[Route("api/authentication")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] //"Bearer"
public class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
{
    private readonly IAuthenticationService _authenticationService = authenticationService;

    [AllowAnonymous, HttpPost("listeners/register")]
    public async Task<IActionResult> RegisterListenerAsync([FromBody] ListenerRegisterRequest registerRequest)
    {
        await _authenticationService.RegisterListenerAsync(registerRequest);
        return Created();
    }

    // [AllowAnonymous, HttpPost("login")]

    // [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("change-password")]

    // [AllowAnonymous, HttpPost("forgot-password")]
}
