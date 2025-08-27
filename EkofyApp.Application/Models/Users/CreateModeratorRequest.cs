namespace EkofyApp.Application.Models.Users;
public sealed record class CreateModeratorRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}
