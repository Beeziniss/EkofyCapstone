namespace EkofyApp.Application.Models.Chat;
public class MessageResponse
{
    public string Id { get; set; }
    public string SenderId { get; set; }
    public string Text { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; } // nếu cần hiển thị "Đã thu hồi"
}
