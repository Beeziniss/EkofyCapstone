namespace EkofyApp.Application.Models.Chat;
public sealed record MessageResponse
{
    public string Id { get; init; }
    public string SenderId { get; init; }
    public string Text { get; init; }
    public DateTimeOffset SentAt { get; init; }
    public bool IsRead { get; init; }
    public bool IsDeleted { get; init; } // nếu cần hiển thị "Đã thu hồi"
}
