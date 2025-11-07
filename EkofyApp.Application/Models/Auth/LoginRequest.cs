namespace EkofyApp.Application.Models.Auth;
public sealed record class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool IsRememberMe { get; set; } = false;
    //public string? RecaptchaToken { get; set; } // Optional, used for bot protection
}
