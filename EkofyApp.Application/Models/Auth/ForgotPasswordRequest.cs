namespace EkofyApp.Application.Models.Auth;

public sealed record class ForgotPasswordRequest
{
    public string Email { get; init; } = null!;
}