using EkofyApp.Domain.Utils;

namespace EkofyApp.Application.Models.Notifications;

public sealed record class NotificationResponse
{
    public string Content { get; init; } = null!;
    public string Avatar { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; } = HelperMethod.GetUtcPlus7TimeOffset();
}
