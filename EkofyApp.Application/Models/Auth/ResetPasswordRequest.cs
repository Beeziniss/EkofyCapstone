namespace EkofyApp.Application.Models.Auth;

public sealed record class ResetPasswordRequest
{
    public string Email { get; init; } = null!;
    public string OtpCode { get; init; } = null!;
    public string NewPassword { get; init; } = null!;
    public string ConfirmPassword { get; init; } = null!;
}