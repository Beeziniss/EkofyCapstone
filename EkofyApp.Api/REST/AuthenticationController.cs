using EkofyApp.Api.Filters;
using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;
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

    #region Listeners
    [AllowAnonymous, HttpPost("register/listener")]
    public async Task<IActionResult> RegisterListenerAsync([FromBody] ListenerRegisterRequest registerRequest)
    {
        var validationResult = new ListenerRegisterRequestValidator().Validate(registerRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);

            return BadRequest(problemDetails);
        }

        await _authenticationService.RegisterListenerAsync(registerRequest);
        return Created();
    }

    [AllowAnonymous, HttpPost("login/listener")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest loginRequest)
    {
        var validationResult = new LoginRequestValidator().Validate(loginRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);
            return BadRequest(problemDetails);
        }

        var result = await _authenticationService.LoginListenerAsync(loginRequest);
        return Ok(new { Message = "Login Successfully", result });
    }
    #endregion

    #region Artists
    [AllowAnonymous, HttpPost("register/artist")]
    public async Task<IActionResult> RegisterArtistAsync([FromBody] ArtistRegisterRequest registerRequest)
    {
        var validationResult = new ArtistRegisterRequestValidator().Validate(registerRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);
            return BadRequest(problemDetails);
        }

        await _authenticationService.RegisterArtistAsync(registerRequest);
        return Created();
    }

    [AllowAnonymous, HttpPost("login/artist")]
    public async Task<IActionResult> LoginArtistAsync([FromBody] LoginRequest loginRequest)
    {
        var validationResult = new LoginRequestValidator().Validate(loginRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);
            return BadRequest(problemDetails);
        }

        var result = await _authenticationService.LoginArtistAsync(loginRequest);
        return Ok(new { Message = "Login Successfully", result });
    }
    #endregion

    [AllowAnonymous, HttpPost("login/moderator")]
    public async Task<IActionResult> LoginModeratorAsync([FromBody] LoginRequest loginRequest)
    {
        var validationResult = new LoginRequestValidator().Validate(loginRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);
            return BadRequest(problemDetails);
        }

        var result = await _authenticationService.LoginModeratorAsync(loginRequest);
        return Ok(new { Message = "Login Successfully", result });
    }

    [AllowAnonymous, HttpPost("login/admin")]
    public async Task<IActionResult> LoginAdminAsync([FromBody] LoginRequest loginRequest)
    {
        var validationResult = new LoginRequestValidator().Validate(loginRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);
            return BadRequest(problemDetails);
        }

        var result = await _authenticationService.LoginAdminAsync(loginRequest);
        return Ok(new { Message = "Login Successfully", result });
    }

    // [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("change-password")]

    // [AllowAnonymous, HttpPost("forgot-password")]
}
