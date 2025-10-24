namespace EkofyApp.Application.Models.Users;
public sealed record class CreateAdminRequest
{
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}
