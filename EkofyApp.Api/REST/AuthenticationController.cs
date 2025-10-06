using EkofyApp.Api.Filters;
using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.REST;
[Route("api/authentication")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] //"Bearer"
public class AuthenticationController(IAuthenticationService authenticationService, IUserSubscriptionService userSubscriptionService) : ControllerBase
{
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly IUserSubscriptionService _userSubscriptionService = userSubscriptionService;

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

    // TODO: Cân nhắc cho đăng nhập chung giữa listener và artist
    // Resolved: Không cần vì xét về UI thì sẽ có button riêng cho listener và artist
    // Nếu là artist thì vẫn vào trang chủ của web app được và vào trang quản lý của nghệ sĩ được
    // Nhưng nếu là listener thì không thể vào trang quản lý của nghệ sĩ được
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

    [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("users/me")]
    public async Task<IActionResult> GetCurrentUserProfileAsync()
    {
        var result = await _authenticationService.GetCurrentUserProfileAsync();
        return Ok(new { Message = "Retrieved current user profile successfully", result });
    }

    // [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("change-password")]

    // [AllowAnonymous, HttpPost("forgot-password")]

    [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshTokenAsync() 
    {
        var result = await _authenticationService.RefreshNewTokenAsync();
        return Ok(new { Message = "Refresh Token Successfully", result });
    }


    [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        await _authenticationService.LogoutAsync();
        return Ok(new { Message = "Logout Successfully" });
    }

    [AllowAnonymous, HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtpAsync(string email)
    {
        await _authenticationService.ResendOtpAsync(email);

        return Ok(new { Message = "Resend OTP Successfully" });
    }

    [AllowAnonymous, HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtpAsync(string email, string providedOtp)
    {
        await _authenticationService.VerifyOtpAsync(email, providedOtp);

        return Ok(new { Message = "Verify OTP Successfully" });
    }
}
