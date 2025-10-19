using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class Restriction // TODO: Chưa rõ cách sử dụng vì có status trong User rồi nên cái này có thể thay thế cho report "stuff"
{
    public RestrictionType Type { get; set; } = RestrictionType.None;
    public RestrictionAction? Action { get; set; } // Hành động đã thực hiện để dẫn đến việc bị hạn chế
    public string? Reason { get; set; }
    public DateTimeOffset? RestrictedAt { get; set; }
    public DateTimeOffset? Expired { get; set; }
}
