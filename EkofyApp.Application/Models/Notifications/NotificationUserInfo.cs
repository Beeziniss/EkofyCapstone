namespace EkofyApp.Application.Models.Notifications;

public sealed record class NotificationUserInfo
{
    public string Name { get; init; } = null!; // User's display name
    public string Avatar { get; init; } = null!; // URL to the user's avatar image
}
