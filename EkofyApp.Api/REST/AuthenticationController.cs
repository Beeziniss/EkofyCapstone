using EkofyApp.Api.Filters;
using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;
using EkofyApp.Application.ServiceInterfaces.Authentication;
using EkofyApp.Application.ServiceInterfaces.UserSubscriptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace EkofyApp.Api.REST;
[Route("api/authentication")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserSubscriptionService _userSubscriptionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        IUserSubscriptionService userSubscriptionService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authenticationService = authenticationService;
        _userSubscriptionService = userSubscriptionService;
        _httpContextAccessor = httpContextAccessor;
    }

    [AllowAnonymous, HttpGet("test-ip-address")]
    public IActionResult TestIpAddress()
    {
        var ipXFowardAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        var ipRemoteAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        Log.Error("Test IP X-Foward: {IP}", HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString());
        Log.Error("Test IP Remote: {IP}", HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(new { XFoward = ipXFowardAddress, Remote = ipRemoteAddress , Message = "Test IP Address Successfully" });
    }

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

    [AllowAnonymous, HttpPost("login/listener/google")]
    public async Task<IActionResult> LoginWithGoogleAsync([FromBody] LoginGoogleRequest loginGoogleRequest)
    {
        var result = await _authenticationService.LoginByGoogleAsync(loginGoogleRequest);
        return Ok(new { Message = "Login with Google Successfully", result });
    }

    [Authorize(Roles = "Listener"), HttpPatch("listener/link-google")]
    public async Task<IActionResult> LinkGoogleAccountAsync()
    {
        await _authenticationService.LinkWithGoogleAccountAsync();
        return Ok(new { Message = "Link Google Account Successfully" });
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

    [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("change-password")]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest changePasswordRequest)
    {
        var validationResult = new ChangePasswordRequestValidator().Validate(changePasswordRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);
            return BadRequest(problemDetails);
        }

        await _authenticationService.ChangePasswordAsync(changePasswordRequest);

        return Ok(new { Message = "Password changed successfully" });
    }

    [AllowAnonymous, HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshTokenAsync() 
    {
        var result = await _authenticationService.RefreshNewTokenAsync();

        // Assuming result contains RefreshToken property
        var refreshToken = result?.RefreshToken;
        if (!string.IsNullOrEmpty(refreshToken))
        {
            CookieOptions cookieOptions = new()
            {
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                MaxAge = TimeSpan.FromDays(7)
            };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
        }

        return Ok(new { Message = "Refresh Token Successfully", result });
    }


    [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(bool isMobile = false)
    {
        await _authenticationService.LogoutAsync(isMobile);
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

    [AllowAnonymous, HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest forgotPasswordRequest)
    {
        var validationResult = new ForgotPasswordRequestValidator().Validate(forgotPasswordRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);
            return BadRequest(problemDetails);
        }

        await _authenticationService.ForgotPasswordAsync(forgotPasswordRequest);

        return Ok(new { Message = "Reset password OTP sent to your email successfully" });
    }

    [AllowAnonymous, HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest resetPasswordRequest)
    {
        var validationResult = new ResetPasswordRequestValidator().Validate(resetPasswordRequest);
        if (!validationResult.IsValid)
        {
            string instance = HttpContext.Request.Path;
            var problemDetails = FluentValidationFilter.ToProblemDetails(validationResult, instance);
            return BadRequest(problemDetails);
        }

        await _authenticationService.ResetPasswordAsync(resetPasswordRequest);

        return Ok(new { Message = "Password reset successfully" });
    }
}
