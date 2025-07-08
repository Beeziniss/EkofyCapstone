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

    //[AllowAnonymous, HttpPost("register")]

    // [AllowAnonymous, HttpPost("login")]

    // [Authorize(Roles = "Listener,Artist,Moderator,Admin"), HttpPost("change-password")]

    // [AllowAnonymous, HttpPost("forgot-password")]
}
