namespace EkofyApp.Application.Models.Auth;

public sealed record class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = null!;
    public string NewPassword { get; init; } = null!;
    public string ConfirmPassword { get; init; } = null!;
}